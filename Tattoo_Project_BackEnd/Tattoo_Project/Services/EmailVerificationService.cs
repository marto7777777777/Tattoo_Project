using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class EmailVerificationService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ITokenService tokenService,
        IConfiguration configuration,
        TimeProvider timeProvider)
        : IEmailVerificationService
    {
        private const int CodeLifetimeMinutes = 10;
        private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

        public async Task<ResultService> StartRegistrationAsync(RegisterDto dto)
        {
            var currentTermsVersion = configuration["Legal:TermsVersion"];
            var currentPrivacyVersion = configuration["Legal:PrivacyVersion"];
            if (!dto.AcceptTermsAndPrivacy || string.IsNullOrWhiteSpace(currentTermsVersion) || string.IsNullOrWhiteSpace(currentPrivacyVersion) ||
                !string.Equals(dto.TermsVersion, currentTermsVersion, StringComparison.Ordinal) ||
                !string.Equals(dto.PrivacyVersion, currentPrivacyVersion, StringComparison.Ordinal))
                return ResultService.Fail("You must accept the current Terms of Service and Privacy Policy.");
            dto.Email = dto.Email.Trim();
            dto.UserName = dto.UserName.Trim();
            dto.FirstName = dto.FirstName.Trim();
            dto.LastName = dto.LastName.Trim();

            var normalizedEmail = userManager.NormalizeEmail(dto.Email);
            var normalizedUserName = userManager.NormalizeName(dto.UserName);
            var now = UtcNow;

            // Never delete an existing Identity account during registration cleanup.
            // Only PendingRegistration rows are eligible for expiry cleanup.
            var existingByEmail = await userManager.FindByEmailAsync(dto.Email);
            if (existingByEmail != null)
                return ResultService.Fail("Email is already registered.");

            var existingByName = await userManager.FindByNameAsync(dto.UserName);
            if (existingByName != null)
                return ResultService.Fail("Username is already taken.");

            var candidate = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.UserName,
                Email = dto.Email
            };

            foreach (var validator in userManager.UserValidators)
            {
                var validation = await validator.ValidateAsync(userManager, candidate);
                if (!validation.Succeeded)
                {
                    return ResultService.Fail(string.Join(" ", validation.Errors.Select(e => e.Description)));
                }
            }

            foreach (var validator in userManager.PasswordValidators)
            {
                var validation = await validator.ValidateAsync(userManager, candidate, dto.Password);
                if (!validation.Succeeded)
                {
                    return ResultService.Fail(string.Join(" ", validation.Errors.Select(e => e.Description)));
                }
            }

            var expired = await context.PendingRegistrations
                .Where(p => p.ExpiresAt < now)
                .ToListAsync();
            context.PendingRegistrations.RemoveRange(expired);
            if (expired.Count > 0)
            {
                await context.SaveChangesAsync();
            }

            var conflictingName = await context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.NormalizedUserName == normalizedUserName && p.NormalizedEmail != normalizedEmail);
            if (conflictingName != null)
            {
                return ResultService.Fail("Username is already awaiting verification.");
            }

            var pending = await context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.NormalizedEmail == normalizedEmail);

            if (pending == null)
            {
                pending = new PendingRegistration();
                context.PendingRegistrations.Add(pending);
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            pending.FirstName = dto.FirstName;
            pending.LastName = dto.LastName;
            pending.UserName = dto.UserName;
            pending.NormalizedUserName = normalizedUserName;
            pending.Email = dto.Email;
            pending.NormalizedEmail = normalizedEmail;
            pending.PasswordHash = userManager.PasswordHasher.HashPassword(candidate, dto.Password);
            pending.VerificationCodeHash = HashPendingCode(pending.Id, code);
            pending.CreatedAt = now;
            pending.ExpiresAt = now.AddMinutes(CodeLifetimeMinutes);
            pending.FailedVerificationAttempts = 0;
            pending.TermsAcceptedAt = now;
            pending.PrivacyAcceptedAt = now;
            pending.TermsVersion = currentTermsVersion;
            pending.PrivacyVersion = currentPrivacyVersion;

            await context.SaveChangesAsync();

            try
            {
                await emailService.SendEmailAsync(
                    pending.Email,
                    GetSubject(EmailVerificationPurpose.Register),
                    BuildEmailHtml(pending.FirstName, code, EmailVerificationPurpose.Register));
            }
            catch
            {
                context.PendingRegistrations.Remove(pending);
                await context.SaveChangesAsync();
                return ResultService.Fail("Verification code could not be sent.");
            }

            return ResultService.Ok();
        }

        public async Task<ResultService> SendCodeAsync(ApplicationUser user, EmailVerificationPurpose purpose)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var now = UtcNow;

            var oldCodes = await context.EmailVerificationCodes
                .Where(c => c.UserId == user.Id && c.Purpose == purpose && c.UsedAt == null)
                .ToListAsync();

            foreach (var oldCode in oldCodes)
            {
                oldCode.UsedAt = now;
            }

            context.EmailVerificationCodes.Add(new EmailVerificationCode
            {
                UserId = user.Id,
                Purpose = purpose,
                CodeHash = HashVerificationCode(user.Id, purpose, code),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(CodeLifetimeMinutes)
            });

            await context.SaveChangesAsync();

            await emailService.SendEmailAsync(
                user.Email!,
                GetSubject(purpose),
                BuildEmailHtml(user.FirstName, code, purpose));

            return ResultService.Ok();
        }

        public async Task<ResultService<AuthResponseDto>> VerifyRegisterCodeAsync(string email, string code)
        {
            var normalizedEmail = userManager.NormalizeEmail(email.Trim());
            var pending = await context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.NormalizedEmail == normalizedEmail);

            if (pending == null || pending.ExpiresAt < UtcNow)
            {
                return ResultService<AuthResponseDto>.Fail("Invalid or expired verification code.");
            }
            if (!IsPendingCodeValid(pending, code))
            {
                pending.FailedVerificationAttempts++;
                if (pending.FailedVerificationAttempts >= 5) context.PendingRegistrations.Remove(pending);
                await context.SaveChangesAsync();
                return ResultService<AuthResponseDto>.Fail("Invalid or expired verification code.");
            }

            if (await userManager.FindByEmailAsync(pending.Email) != null)
            {
                return ResultService<AuthResponseDto>.Fail("Email is already registered.");
            }

            if (await userManager.FindByNameAsync(pending.UserName) != null)
            {
                return ResultService<AuthResponseDto>.Fail("Username is already taken.");
            }

            var user = new ApplicationUser
            {
                FirstName = pending.FirstName,
                LastName = pending.LastName,
                UserName = pending.UserName,
                Email = pending.Email,
                EmailConfirmed = true,
                PasswordHash = pending.PasswordHash,
                TermsAcceptedAt = pending.TermsAcceptedAt,
                PrivacyAcceptedAt = pending.PrivacyAcceptedAt,
                TermsVersion = pending.TermsVersion,
                PrivacyVersion = pending.PrivacyVersion
            };

            await using var transaction = await context.Database.BeginTransactionAsync();
            IdentityResult createResult;
            try
            {
                createResult = await userManager.CreateAsync(user);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
                sqlException.Number is 2601 or 2627)
            {
                return ResultService<AuthResponseDto>.Fail("Email or username is already registered.");
            }

            if (!createResult.Succeeded)
            {
                return ResultService<AuthResponseDto>.Fail(
                    string.Join(" ", createResult.Errors.Select(e => e.Description)));
            }

            context.PendingRegistrations.Remove(pending);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            var token = await tokenService.GenerateJwtTokenAsync(user);
            var roles = await userManager.GetRolesAsync(user);

            return ResultService<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles
                }
            });
        }

        public async Task<ResultService> ResendRegisterCodeAsync(string email)
        {
            var normalizedEmail = userManager.NormalizeEmail(email.Trim());
            var pending = await context.PendingRegistrations
                .FirstOrDefaultAsync(p => p.NormalizedEmail == normalizedEmail);

            if (pending == null)
            {
                return ResultService.Fail("Pending registration was not found. Please register again.");
            }

            if (await userManager.FindByEmailAsync(pending.Email) != null)
            {
                return ResultService.Fail("Email is already verified.");
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var now = UtcNow;
            pending.VerificationCodeHash = HashPendingCode(pending.Id, code);
            pending.CreatedAt = now;
            pending.ExpiresAt = now.AddMinutes(CodeLifetimeMinutes);
            pending.FailedVerificationAttempts = 0;
            await context.SaveChangesAsync();

            try
            {
                await emailService.SendEmailAsync(
                    pending.Email,
                    GetSubject(EmailVerificationPurpose.Register),
                    BuildEmailHtml(pending.FirstName, code, EmailVerificationPurpose.Register));
            }
            catch
            {
                context.PendingRegistrations.Remove(pending);
                await context.SaveChangesAsync();
                return ResultService.Fail("Verification code could not be sent. Please register again.");
            }

            return ResultService.Ok();
        }

        public async Task<ResultService> SendForgotPasswordCodeAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                // Keep the response indistinguishable from an existing account.
                // This prevents the password-reset endpoint from becoming an
                // account enumeration oracle.
                return ResultService.Ok();
            }

            return await SendCodeAsync(user, EmailVerificationPurpose.PasswordReset);
        }

        public async Task<ResultService> VerifyPasswordResetCodeAsync(string email, string code)
        {
            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return ResultService.Fail("Invalid email or verification code.");
            }

            return await CheckCodeWithoutUsingAsync(user, EmailVerificationPurpose.PasswordReset, code);
        }

        public async Task<ResultService> ResetPasswordWithCodeAsync(string email, string code, string newPassword, string confirmNewPassword)
        {
            if (newPassword != confirmNewPassword)
            {
                return ResultService.Fail("Passwords do not match.");
            }

            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return ResultService.Fail("Invalid email or verification code.");
            }

            var validation = await ValidateCodeAsync(user, EmailVerificationPurpose.PasswordReset, code);
            if (!validation.Success)
            {
                return validation;
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return ResultService.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
            }
            user.TokenVersion++;
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded) return ResultService.Fail(string.Join(" ", stampResult.Errors.Select(e => e.Description)));
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return ResultService.Fail(string.Join(" ", updateResult.Errors.Select(e => e.Description)));

            return ResultService.Ok();
        }

        public async Task<ResultService> SendPasswordChangeCodeAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ResultService.Fail("User was not found.");
            }

            return await SendCodeAsync(user, EmailVerificationPurpose.PasswordChange);
        }

        public async Task<ResultService> ChangePasswordWithCodeAsync(string userId, string code, string newPassword, string confirmNewPassword)
        {
            if (newPassword != confirmNewPassword)
            {
                return ResultService.Fail("Passwords do not match.");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ResultService.Fail("User was not found.");
            }

            var validation = await ValidateCodeAsync(user, EmailVerificationPurpose.PasswordChange, code);
            if (!validation.Success)
            {
                return validation;
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return ResultService.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
            }
            user.TokenVersion++;
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded) return ResultService.Fail(string.Join(" ", stampResult.Errors.Select(e => e.Description)));
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return ResultService.Fail(string.Join(" ", updateResult.Errors.Select(e => e.Description)));

            return ResultService.Ok();
        }

        public async Task<ResultService> RequestEmailChangeAsync(string userId, string newEmail)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return ResultService.Fail("User was not found.");
            newEmail = (newEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(newEmail)) return ResultService.Fail("Email is required.");
            if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase)) return ResultService.Fail("This is already your email address.");
            var existing = await userManager.FindByEmailAsync(newEmail);
            if (existing != null && existing.Id != userId) return ResultService.Fail("Email is already registered.");

            var normalized = userManager.NormalizeEmail(newEmail);
            var now = UtcNow;
            var recent = await context.PendingEmailChanges
                .Where(x => x.UserId == userId && x.UsedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            if (recent != null && recent.CreatedAt > now.AddSeconds(-60))
                return ResultService.Fail("Please wait before requesting another email-change code.");

            var stale = await context.PendingEmailChanges.Where(x => x.UserId == userId && x.UsedAt == null).ToListAsync();
            foreach (var item in stale) item.UsedAt = now;

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var request = new PendingEmailChange
            {
                UserId = userId,
                NewEmail = newEmail,
                NormalizedNewEmail = normalized,
                CodeHash = HashEmailChangeCode(userId, normalized, code),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(CodeLifetimeMinutes)
            };
            context.PendingEmailChanges.Add(request);
            await context.SaveChangesAsync();
            await emailService.SendEmailAsync(newEmail, "Confirm your new InkRoute email", BuildEmailHtml(user.FirstName, code, EmailVerificationPurpose.EmailChange));
            return ResultService.Ok();
        }

        public async Task<ResultService> ConfirmEmailChangeAsync(string userId, string newEmail, string code)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return ResultService.Fail("User was not found.");
            newEmail = (newEmail ?? string.Empty).Trim();
            var normalized = userManager.NormalizeEmail(newEmail);
            var now = UtcNow;
            var request = await context.PendingEmailChanges
                .Where(x => x.UserId == userId && x.NormalizedNewEmail == normalized && x.UsedAt == null && x.ExpiresAt >= now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            if (request == null) return ResultService.Fail("Invalid or expired email-change code.");
            if (request.FailedAttempts >= 5) return ResultService.Fail("Invalid or expired email-change code.");
            if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6 || !code.Trim().All(char.IsDigit))
                return ResultService.Fail("Invalid or expired email-change code.");

            var expected = Convert.FromHexString(request.CodeHash);
            var actual = Convert.FromHexString(HashEmailChangeCode(userId, normalized, code.Trim()));
            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                request.FailedAttempts++;
                if (request.FailedAttempts >= 5) request.UsedAt = now;
                await context.SaveChangesAsync();
                return ResultService.Fail("Invalid or expired email-change code.");
            }

            var existing = await userManager.FindByEmailAsync(newEmail);
            if (existing != null && existing.Id != userId) return ResultService.Fail("Email is already registered.");

            await using var transaction = await context.Database.BeginTransactionAsync();
            var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            var changed = await userManager.ChangeEmailAsync(user, newEmail, token);
            if (!changed.Succeeded) return ResultService.Fail(string.Join(" ", changed.Errors.Select(e => e.Description)));
            user.EmailConfirmed = true;
            user.TokenVersion++;
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded) return ResultService.Fail(string.Join(" ", stampResult.Errors.Select(e => e.Description)));
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return ResultService.Fail(string.Join(" ", updateResult.Errors.Select(e => e.Description)));

            var client = await context.Clients.FirstOrDefaultAsync(x => x.UserId == userId);
            if (client != null) client.Email = newEmail;
            var artist = await context.TattooArtists.FirstOrDefaultAsync(x => x.UserId == userId);
            if (artist != null) artist.Email = newEmail;
            request.UsedAt = now;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        private string HashEmailChangeCode(string userId, string normalizedEmail, string code)
            => Convert.ToHexString(HashCodeWithApplicationSecret($"email-change:{userId}:{normalizedEmail}:{code}"));

        private async Task<ResultService> ValidateCodeAsync(ApplicationUser user, EmailVerificationPurpose purpose, string code)
        {
            var verificationCode = await FindValidCodeAsync(user, purpose, code);
            if (verificationCode == null)
            {
                return ResultService.Fail("Invalid or expired verification code.");
            }

            verificationCode.UsedAt = UtcNow;
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        private async Task<ResultService> CheckCodeWithoutUsingAsync(ApplicationUser user, EmailVerificationPurpose purpose, string code)
        {
            var verificationCode = await FindValidCodeAsync(user, purpose, code);
            return verificationCode == null
                ? ResultService.Fail("Invalid or expired verification code.")
                : ResultService.Ok();
        }

        private async Task<EmailVerificationCode?> FindValidCodeAsync(ApplicationUser user, EmailVerificationPurpose purpose, string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6 || !code.Trim().All(char.IsDigit))
            {
                return null;
            }

            var now = UtcNow;

            var verificationCode = await context.EmailVerificationCodes
                .Where(c =>
                    c.UserId == user.Id &&
                    c.Purpose == purpose &&
                    c.UsedAt == null &&
                    c.ExpiresAt >= now)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
            if (verificationCode == null) return null;
            var expected = Convert.FromHexString(verificationCode.CodeHash);
            var actual = Convert.FromHexString(HashVerificationCode(user.Id, purpose, code.Trim()));
            if (CryptographicOperations.FixedTimeEquals(expected, actual)) return verificationCode;
            verificationCode.FailedAttempts++;
            if (verificationCode.FailedAttempts >= 5) verificationCode.UsedAt = now;
            await context.SaveChangesAsync();
            return null;
        }

        private string HashVerificationCode(string userId, EmailVerificationPurpose purpose, string code)
        {
            var raw = $"{userId}:{(int)purpose}:{code}";
            var bytes = HashCodeWithApplicationSecret(raw);
            return Convert.ToHexString(bytes);
        }

        private string HashPendingCode(Guid pendingId, string code)
        {
            var raw = $"{pendingId:N}:{code}";
            var bytes = HashCodeWithApplicationSecret(raw);
            return Convert.ToHexString(bytes);
        }

        private bool IsPendingCodeValid(PendingRegistration pending, string code)
        {
            var normalizedCode = code?.Trim();
            if (normalizedCode == null || normalizedCode.Length != 6 || !normalizedCode.All(char.IsDigit))
            {
                return false;
            }

            var expected = Convert.FromHexString(pending.VerificationCodeHash);
            var actual = Convert.FromHexString(HashPendingCode(pending.Id, normalizedCode));
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }

        private byte[] HashCodeWithApplicationSecret(string raw)
        {
            var secret = configuration["Verification:CodeHashSecret"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new InvalidOperationException("Verification:CodeHashSecret must contain at least 32 characters.");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        }

        private static string GetSubject(EmailVerificationPurpose purpose)
            => purpose switch
            {
                EmailVerificationPurpose.Register => "Your InkRoute verification code",
                EmailVerificationPurpose.PasswordReset => "Your InkRoute password reset code",
                EmailVerificationPurpose.PasswordChange => "Your InkRoute password change code",
                EmailVerificationPurpose.EmailChange => "Confirm your new InkRoute email",
                _ => "Your InkRoute verification code"
            };

        private static string BuildEmailHtml(string firstName, string code, EmailVerificationPurpose purpose)
        {
            var title = purpose switch
            {
                EmailVerificationPurpose.Register => "Verify your email",
                EmailVerificationPurpose.PasswordReset => "Reset your password",
                EmailVerificationPurpose.PasswordChange => "Change your password",
                EmailVerificationPurpose.EmailChange => "Confirm your new email",
                _ => "Verify your email"
            };

            var subtitle = purpose switch
            {
                EmailVerificationPurpose.Register => "Use this code to finish creating your InkRoute account.",
                EmailVerificationPurpose.PasswordReset => "Use this code to reset your InkRoute password.",
                EmailVerificationPurpose.PasswordChange => "Use this code to confirm your InkRoute password change.",
                EmailVerificationPurpose.EmailChange => "Use this code to confirm that you own this new email address.",
                _ => "Use this code to continue with InkRoute."
            };

            var safeFirstName = WebUtility.HtmlEncode(firstName);

            return $"""
                <!doctype html>
                <html>
                  <body style="margin:0;padding:0;background:#070707;color:#f7f2ec;font-family:Arial,Helvetica,sans-serif;">
                    <div style="background:#070707;padding:36px 16px;">
                      <div style="max-width:620px;margin:0 auto;background:#151517;border:1px solid rgba(255,255,255,.14);border-radius:26px;overflow:hidden;box-shadow:0 24px 70px rgba(0,0,0,.45);">
                        <div style="padding:28px 30px;background:linear-gradient(135deg,rgba(217,107,79,.28),rgba(242,192,107,.10));border-bottom:1px solid rgba(255,255,255,.10);">
                          <div style="color:#f2c06b;font-size:13px;font-weight:900;letter-spacing:2px;text-transform:uppercase;margin-bottom:10px;">InkRoute</div>
                          <h1 style="margin:0;color:#f7f2ec;font-size:30px;line-height:1.05;letter-spacing:-.04em;">{title}</h1>
                        </div>

                        <div style="padding:32px 30px 34px;">
                          <p style="margin:0 0 14px;color:#d8d1c7;font-size:16px;line-height:1.6;">Hi {safeFirstName},</p>
                          <p style="margin:0 0 26px;color:#d8d1c7;font-size:16px;line-height:1.6;">{subtitle}</p>

                          <div style="text-align:center;margin:28px 0;">
                            <div style="display:inline-block;background:#0b0b0c;border:1px solid rgba(242,192,107,.40);border-radius:22px;padding:22px 26px;box-shadow:0 18px 42px rgba(0,0,0,.35);">
                              <div style="color:#a99f95;font-size:12px;font-weight:900;letter-spacing:2px;text-transform:uppercase;margin-bottom:12px;">Your code</div>
                              <div style="font-size:48px;line-height:1;font-weight:900;letter-spacing:12px;color:#f2c06b;">{code}</div>
                            </div>
                          </div>

                          <p style="margin:24px 0 0;color:#d8d1c7;font-size:15px;line-height:1.6;">This code expires in <strong style="color:#f7f2ec;">10 minutes</strong>.</p>
                          <p style="margin:10px 0 0;color:#a99f95;font-size:14px;line-height:1.6;">If you did not request this code, you can safely ignore this email.</p>
                        </div>

                        <div style="padding:18px 30px;border-top:1px solid rgba(255,255,255,.09);background:#101012;color:#7f756d;font-size:13px;line-height:1.5;">
                          InkRoute Team
                        </div>
                      </div>
                    </div>
                  </body>
                </html>
                """;
        }
    }
}
