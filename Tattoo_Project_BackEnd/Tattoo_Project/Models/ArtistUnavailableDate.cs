namespace Tattoo_Project.Models
{
    public class ArtistUnavailableDate
    {
        public int Id { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public int TattooArtistId { get; set; }

        public TattooArtist TattooArtist { get; set; } = null!;
    }
}