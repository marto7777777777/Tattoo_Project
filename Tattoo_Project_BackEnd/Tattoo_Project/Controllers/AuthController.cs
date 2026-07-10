using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IEmailVerificationService emailVerificationService)
        : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            dto.Email = dto.Email.Trim();
            dto.UserName = dto.UserName.Trim();
            dto.FirstName = dto.FirstName.Trim();
            dto.LastName = dto.LastName.Trim();

            var existingUserByEmail = await userManager.FindByEmailAsync(dto.Email);
            var existingUserByName = await userManager.FindByNameAsync(dto.UserName);

            if (existingUserByEmail != null)
            {
                if (existingUserByEmail.EmailConfirmed)
                {
                    return BadRequest("Email is already registered.");
                }

                if (existingUserByName != null && existingUserByName.Id != existingUserByEmail.Id)
                {
                    return BadRequest("Username is already taken.");
                }

                existingUserByEmail.FirstName = dto.FirstName;
                existingUserByEmail.LastName = dto.LastName;
                existingUserByEmail.UserName = dto.UserName;
                existingUserByEmail.NormalizedUserName = userManager.NormalizeName(dto.UserName);

                var updateResult = await userManager.UpdateAsync(existingUserByEmail);

                if (!updateResult.Succeeded)
                {
                    return BadRequest(updateResult.Errors);
                }

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUserByEmail);
                var passwordResult = await userManager.ResetPasswordAsync(existingUserByEmail, resetToken, dto.Password);

                if (!passwordResult.Succeeded)
                {
                    return BadRequest(passwordResult.Errors);
                }

                var resendResult = await emailVerificationService.SendCodeAsync(
                    existingUserByEmail,
                    EmailVerificationPurpose.Register);

                if (!resendResult.Success)
                {
                    return BadRequest(resendResult.ErrorMessage);
                }

                return Ok(new
                {
                    message = "This email already has an unverified account. A new 6-digit verification code was sent.",
                    email = existingUserByEmail.Email
                });
            }

            if (existingUserByName != null)
            {
                return BadRequest("Username is already taken.");
            }

            ApplicationUser user = new()
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.UserName,
                Email = dto.Email,
                EmailConfirmed = false
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var codeResult = await emailVerificationService.SendCodeAsync(
                user,
                EmailVerificationPurpose.Register);

            if (!codeResult.Success)
            {
                await userManager.DeleteAsync(user);
                return BadRequest(codeResult.ErrorMessage);
            }

            return Ok(new
            {
                message = "Registration successful. Please check your email for the 6-digit verification code.",
                email = user.Email
            });
        }

        [HttpPost("register/verify-code")]
        public async Task<IActionResult> VerifyRegisterCode(VerifyRegisterCodeDto dto)
        {
            var result = await emailVerificationService.VerifyRegisterCodeAsync(dto.Email, dto.Code);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [HttpPost("register/resend-code")]
        public async Task<IActionResult> ResendRegisterCode(ResendRegisterCodeDto dto)
        {
            var result = await emailVerificationService.ResendRegisterCodeAsync(dto.Email);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Verification code sent successfully.");
        }

        [HttpPost("forgot-password/send-code")]
        public async Task<IActionResult> SendForgotPasswordCode(ForgotPasswordSendCodeDto dto)
        {
            var result = await emailVerificationService.SendForgotPasswordCodeAsync(dto.Email);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Password reset code sent successfully.");
        }

        [HttpPost("forgot-password/verify-code")]
        public async Task<IActionResult> VerifyForgotPasswordCode(VerifyPasswordResetCodeDto dto)
        {
            var result = await emailVerificationService.VerifyPasswordResetCodeAsync(dto.Email, dto.Code);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Verification code is valid.");
        }

        [HttpPost("forgot-password/reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordWithCodeDto dto)
        {
            var result = await emailVerificationService.ResetPasswordWithCodeAsync(
                dto.Email,
                dto.Code,
                dto.NewPassword,
                dto.ConfirmNewPassword);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Password changed successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Login)
                       ?? await userManager.FindByNameAsync(dto.Login);

            if (user == null)
            {
                return Unauthorized("Invalid login or password.");
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordValid)
            {
                return Unauthorized("Invalid login or password.");
            }

            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                return BadRequest("Please verify your email before logging in.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = await tokenService.GenerateJwtTokenAsync(user);

            return Ok(new AuthResponseDto
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
    }
}
