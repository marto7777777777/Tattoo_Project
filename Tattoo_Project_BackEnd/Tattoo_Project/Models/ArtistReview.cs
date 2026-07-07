namespace Tattoo_Project.Models
{
    public class ArtistReview
    {
        public int Id { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public int TattooRequestId { get; set; }

        public TattooRequest TattooRequest { get; set; } = null!;

        public int ClientId { get; set; }

        public Client Client { get; set; } = null!;

        public int TattooArtistId { get; set; }

        public TattooArtist TattooArtist { get; set; } = null!;
    }
}
