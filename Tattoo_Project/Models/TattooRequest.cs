namespace Tattoo_Project.Models
{
    public class TattooRequest
    {
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public string Placement { get; set; } = null!;
        public DateTime CreatedOn { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public RequestStatus Status { get; set; }
        public int RequiredHours { get; set; }
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;
        public int TattooArtistId { get; set; }
        public Consultation? Consultation { get; set; }
        public ICollection<TattooSession> TattooSessions { get; set; } = new List<TattooSession>();
        public TattooArtist TattooArtist { get; set; } = null!;
        public ICollection<TattooReferenceImage> Images { get; set; } = new List<TattooReferenceImage>();
    }
}
