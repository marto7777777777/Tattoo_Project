namespace Tattoo_Project.DTOs.TattooSessionDTOs
{
    public class TattooSessionDto
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal? PriceForTheSession { get; set; }

        public int DurationHours { get; set; }
    }
}
