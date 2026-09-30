using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IEmailVerificationService emailVerificationService,
        ILogger<AuthController> logger)
        : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await emailVerificationService.StartRegistrationAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new
            {
                message = "Verification code sent. Your account will be created after the code is confirmed.",
                email = dto.Email.Trim()
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
            var login = dto.Login.Trim();
            var normalizedEmail = userManager.NormalizeEmail(login);
            var normalizedUserName = userManager.NormalizeName(login);
            var user = await userManager.Users
                .Where(candidate =>
                    candidate.NormalizedEmail == normalizedEmail ||
                    candidate.NormalizedUserName == normalizedUserName)
                .OrderByDescending(candidate => candidate.NormalizedEmail == normalizedEmail)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            if (user == null)
            {
                return Unauthorized("Invalid login or password.");
            }

            var passwordTimer = Stopwatch.StartNew();
            var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);
            passwordTimer.Stop();
            logger.LogInformation(
                "Password verification completed in {ElapsedMs}ms (success: {Success}).",
                passwordTimer.ElapsedMilliseconds,
                isPasswordValid);

            if (!isPasswordValid)
            {
                return Unauthorized("Invalid login or password.");
            }

            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                return BadRequest("Please verify your email before logging in.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = await tokenService.GenerateJwtTokenAsync(user, roles);

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
