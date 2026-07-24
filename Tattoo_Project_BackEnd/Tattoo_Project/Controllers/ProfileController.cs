using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.DTOs.ProfileDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProfileController(
        IProfileService service,
        IEmailVerificationService emailVerificationService) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.GetMyProfileAsync(userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpPatch("user/first-name")]
        public async Task<IActionResult> UpdateFirstName(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdateFirstNameAsync);

        [HttpPatch("user/last-name")]
        public async Task<IActionResult> UpdateLastName(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdateLastNameAsync);

        [HttpPatch("user/email")]
        public async Task<IActionResult> UpdateEmail(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdateEmailAsync);


        [HttpPost("user/password/send-code")]
        public async Task<IActionResult> SendPasswordChangeCode()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await emailVerificationService.SendPasswordChangeCodeAsync(userId);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Password change code sent successfully.");
        }

        [HttpPost("user/password/change")]
        public async Task<IActionResult> ChangePassword(ChangePasswordWithCodeDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await emailVerificationService.ChangePasswordWithCodeAsync(
                userId,
                dto.Code,
                dto.NewPassword,
                dto.ConfirmNewPassword);

            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Password changed successfully.");
        }

        [HttpPatch("contact/profile-image")]
        public async Task<IActionResult> UpdateProfileImage(IFormFile image)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.UpdateProfileImageAsync(userId, image);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpPatch("contact/phone-number")]
        public async Task<IActionResult> UpdatePhoneNumber(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdatePhoneNumberAsync);

        [HttpPatch("contact/city")]
        public async Task<IActionResult> UpdateCity(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdateCityAsync);

        [HttpPatch("contact/country")]
        public async Task<IActionResult> UpdateCountry(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdateCountryAsync);

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("artist/description")]
        public async Task<IActionResult> UpdateDescription(UpdateStringValueDto dto)
            => await RunStringUpdate(dto, service.UpdateDescriptionAsync);

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("consultation/duration")]
        public async Task<IActionResult> UpdateConsultationDuration(UpdateIntValueDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.UpdateConsultationDurationAsync(userId, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Consultation duration updated successfully.");
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("consultation/offers-online")]
        public async Task<IActionResult> UpdateOffersOnlineConsultation(UpdateBoolValueDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.UpdateOffersOnlineConsultationAsync(userId, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Online consultation setting updated successfully.");
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("deposit/requires-deposit")]
        public async Task<IActionResult> UpdateRequiresDeposit(UpdateBoolValueDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.UpdateRequiresDepositAsync(userId, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Deposit requirement updated successfully.");
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("deposit/amount")]
        public async Task<IActionResult> UpdateDepositAmount(UpdateNullableDecimalValueDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.UpdateDepositAmountAsync(userId, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Deposit amount updated successfully.");
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPost("studio/requirements")]
        public async Task<IActionResult> AddRequirement(UpdateStringValueDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.AddRequirementAsync(userId, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPatch("studio/requirements/{id}")]
        public async Task<IActionResult> UpdateRequirement(int id, UpdateStringValueDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.UpdateRequirementAsync(userId, id, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Requirement updated successfully.");
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpDelete("studio/requirements/{id}")]
        public async Task<IActionResult> DeleteRequirement(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.DeleteRequirementAsync(userId, id);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Requirement deleted successfully.");
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPost("portfolio/images")]
        public async Task<IActionResult> AddPortfolioImage(IFormFile image)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.AddPortfolioImageAsync(userId, image);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpDelete("portfolio/images/{id}")]
        public async Task<IActionResult> DeletePortfolioImage(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await service.DeletePortfolioImageAsync(userId, id);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Portfolio image deleted successfully.");
        }

        private async Task<IActionResult> RunStringUpdate(
            UpdateStringValueDto dto,
            Func<string, string, Task<ResultService>> update)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await update(userId, dto.Value);
            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok("Profile updated successfully.");
        }

        private string? GetUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
