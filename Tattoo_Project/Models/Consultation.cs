namespace Tattoo_Project.Models
{
    public class Consultation
    {
        public int Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int TattooRequestId { get; set; }

        public TattooRequest TattooRequest { get; set; } = null!;
    }
}
