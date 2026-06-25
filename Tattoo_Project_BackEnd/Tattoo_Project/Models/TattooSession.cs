namespace Tattoo_Project.Models
{
    public class TattooSession
    {
        public int Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int TattooRequestId { get; set; }

        public decimal PriceForTheSession { get; set; }

        public int DurationHours { get; set; }

        public TattooRequest TattooRequest { get; set; } = null!;
    }
}
