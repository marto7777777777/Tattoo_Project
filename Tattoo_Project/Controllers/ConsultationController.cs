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
            var consultations = await service.GetAllConsultationsAsync();

            return Ok(consultations);
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

            var consultation = await service.GetConsultationByIdAsync(
                id,
                userId,
                User.IsInRole(UserRoles.Admin),
                User.IsInRole(UserRoles.Client),
                User.IsInRole(UserRoles.TattooArtist));

            if (consultation == null)
            {
                return NotFound("Consultation not found.");
            }

            return Ok(consultation);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpPost]
        public async Task<IActionResult> CreateConsultation(CreateConsultationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isCreated = await service.CreateConsultationAsync(dto, userId);

            if (!isCreated)
            {
                return BadRequest("Consultation could not be created.");
            }

            return Ok("Consultation created successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
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

            var isUpdated = await service.UpdateConsultationAsync(
                id,
                dto,
                userId);

            if (!isUpdated)
            {
                return BadRequest("Consultation could not be updated.");
            }

            return Ok("Consultation updated successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsultation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isDeleted = await service.DeleteConsultationAsync(id, userId);

            if (!isDeleted)
            {
                return BadRequest("Consultation could not be deleted.");
            }

            return Ok("Consultation deleted successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
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

            var isCompleted = await service.CompleteConsultationAsync(
                tattooRequestId,
                dto,
                userId);

            if (!isCompleted)
            {
                return BadRequest("Consultation could not be completed.");
            }

            return Ok("Consultation completed successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.TattooArtist)]
        [HttpPut("reject-consultation/{tattooRequestId}")]
        public async Task<IActionResult> RejectConsultation(int tattooRequestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isRejected = await service.RejectConsultationAsync(
                tattooRequestId,
                userId);

            if (!isRejected)
            {
                return BadRequest("Consultation could not be rejected.");
            }

            return Ok("Consultation rejected successfully.");
        }
    }
}