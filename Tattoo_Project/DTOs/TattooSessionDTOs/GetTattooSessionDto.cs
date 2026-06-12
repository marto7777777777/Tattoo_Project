using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.TattooSessionDTOs
{
    public class GetTattooSessionDto
    {

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int TattooRequestId { get; set; }

        public decimal PriceForTheSession { get; set; }

        public int DurationHours { get; set; }
    }
}
