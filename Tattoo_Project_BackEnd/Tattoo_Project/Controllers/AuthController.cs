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
                // Registration never mutates an existing account. Unverified users
                // can request another code through the dedicated resend endpoint.
                return BadRequest("Email is already registered.");
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

            IdentityResult result;
            try
            {
                result = await userManager.CreateAsync(user, dto.Password);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException exception) when (
                exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
                sqlException.Number is 2601 or 2627)
            {
                // The unique database indexes are the final guard if two registration
                // requests for the same email/username arrive at the same time.
                return BadRequest("Email or username is already registered.");
            }

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
