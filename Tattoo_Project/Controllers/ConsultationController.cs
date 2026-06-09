using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.ConsultationDTOs;
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

        [HttpPost]
        public async Task<IActionResult> CreateConsultationAsync(CreateConsultationDto dto)
        {
            var isCreated = await service.CreateConsultationAsync(dto);
            return Ok(isCreated);
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
    }
}
