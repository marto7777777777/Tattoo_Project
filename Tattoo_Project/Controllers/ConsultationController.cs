using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultationController(IConsultationService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GetConsultationDto>>> GetAllConsultationsAsync()
        {
            var consultations = await service.GetAllConsultationsAsync();
            if (consultations == null || !consultations.Any())
            {
                return NotFound("No consultations created yet!");
            }
            return Ok(consultations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetConsultationDto>> GetConsultationByIdAsync(int id)
        {
            var consultation = await service.GetConsultationByIdAsync(id);
            if (consultation == null)
            {
                return NotFound($"Consultation with id {id} doesn't exist!");
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConsultationAsync(int id, UpdateConsultationDto dto)
        {
            var isUpdated = await service.UpdateConsultationAsync(id, dto);

            return Ok(isUpdated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsultationAsync(int id)
        {
            var isDeleted = await service.DeleteConsultationAsync(id);

            return Ok(isDeleted);
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
