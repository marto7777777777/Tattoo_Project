using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.ConsultationDTOs
{
    public class CreateConsultationDto
    {

        public DateTime StartTime { get; set; }

        public int TattooRequestId { get; set; }

        public string? Notes { get; set; }
    }
}
