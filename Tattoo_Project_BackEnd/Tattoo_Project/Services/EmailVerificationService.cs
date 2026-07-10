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
        ITokenService tokenService)
        : IEmailVerificationService
    {
        private const int CodeLifetimeMinutes = 10;

        public async Task<ResultService> SendCodeAsync(ApplicationUser user, EmailVerificationPurpose purpose)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var now = DateTime.UtcNow;

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
            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return ResultService<AuthResponseDto>.Fail("Invalid email or verification code.");
            }

            var validation = await ValidateCodeAsync(user, EmailVerificationPurpose.Register, code);
            if (!validation.Success)
            {
                return ResultService<AuthResponseDto>.Fail(validation.ErrorMessage!);
            }

            user.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ResultService<AuthResponseDto>.Fail(
                    string.Join(" ", updateResult.Errors.Select(e => e.Description)));
            }

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
            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return ResultService.Fail("User was not found.");
            }

            if (await userManager.IsEmailConfirmedAsync(user))
            {
                return ResultService.Fail("Email is already verified.");
            }

            return await SendCodeAsync(user, EmailVerificationPurpose.Register);
        }

        public async Task<ResultService> SendForgotPasswordCodeAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return ResultService.Fail("User was not found.");
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

            return ResultService.Ok();
        }

        private async Task<ResultService> ValidateCodeAsync(ApplicationUser user, EmailVerificationPurpose purpose, string code)
        {
            var verificationCode = await FindValidCodeAsync(user, purpose, code);
            if (verificationCode == null)
            {
                return ResultService.Fail("Invalid or expired verification code.");
            }

            verificationCode.UsedAt = DateTime.UtcNow;
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

            var codeHash = HashVerificationCode(user.Id, purpose, code.Trim());
            var now = DateTime.UtcNow;

            return await context.EmailVerificationCodes
                .Where(c =>
                    c.UserId == user.Id &&
                    c.Purpose == purpose &&
                    c.CodeHash == codeHash &&
                    c.UsedAt == null &&
                    c.ExpiresAt >= now)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private static string HashVerificationCode(string userId, EmailVerificationPurpose purpose, string code)
        {
            var raw = $"{userId}:{(int)purpose}:{code}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }

        private static string GetSubject(EmailVerificationPurpose purpose)
            => purpose switch
            {
                EmailVerificationPurpose.Register => "Your InkRoute verification code",
                EmailVerificationPurpose.PasswordReset => "Your InkRoute password reset code",
                EmailVerificationPurpose.PasswordChange => "Your InkRoute password change code",
                _ => "Your InkRoute verification code"
            };

        private static string BuildEmailHtml(string firstName, string code, EmailVerificationPurpose purpose)
        {
            var title = purpose switch
            {
                EmailVerificationPurpose.Register => "Verify your email",
                EmailVerificationPurpose.PasswordReset => "Reset your password",
                EmailVerificationPurpose.PasswordChange => "Change your password",
                _ => "Verify your email"
            };

            var subtitle = purpose switch
            {
                EmailVerificationPurpose.Register => "Use this code to finish creating your InkRoute account.",
                EmailVerificationPurpose.PasswordReset => "Use this code to reset your InkRoute password.",
                EmailVerificationPurpose.PasswordChange => "Use this code to confirm your InkRoute password change.",
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
