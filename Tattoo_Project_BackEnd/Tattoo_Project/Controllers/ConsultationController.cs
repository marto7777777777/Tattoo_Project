using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultationController(IConsultationService service)
        : ControllerBase
    {
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllConsultations()
        {
            var result = await service.GetAllConsultationsAsync();

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.Client + "," + UserRoles.TattooArtist)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetConsultationById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.GetConsultationByIdAsync(
                id,
                userId,
                User.IsInRole(UserRoles.Admin),
                User.IsInRole(UserRoles.Client),
                User.IsInRole(UserRoles.TattooArtist));

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return Ok(result.Data);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.Client)]
        [HttpPost]
        public async Task<IActionResult> CreateConsultation(CreateConsultationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.CreateConsultationAsync(dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Consultation created successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.Client)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConsultation(
            int id,
            UpdateConsultationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.UpdateConsultationAsync(id, dto, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Consultation updated successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsultation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.DeleteConsultationAsync(id, userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Consultation deleted successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPut("complete-consultation/{tattooRequestId}")]
        public async Task<IActionResult> CompleteConsultation(
            int tattooRequestId,
            CompleteConsultationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.CompleteConsultationAsync(
                tattooRequestId,
                dto,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Consultation completed successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin + "," + UserRoles.TattooArtist)]
        [HttpPut("reject-consultation/{tattooRequestId}")]
        public async Task<IActionResult> RejectConsultation(int tattooRequestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await service.RejectConsultationAsync(
                tattooRequestId,
                userId);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Consultation rejected successfully.");
        }
    }
}