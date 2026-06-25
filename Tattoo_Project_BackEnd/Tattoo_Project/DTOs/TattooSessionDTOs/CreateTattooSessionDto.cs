using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.TattooSessionDTOs
{
    public class CreateTattooSessionDto
    {

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int TattooRequestId { get; set; }

        public int DurationHours { get; set; }
    }
}
