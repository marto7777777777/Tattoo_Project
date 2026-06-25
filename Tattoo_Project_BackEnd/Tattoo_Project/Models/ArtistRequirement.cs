namespace Tattoo_Project.Models
{
    public class ArtistRequirement
    {
        public int Id { get; set; }

        public string Description { get; set; } = null!;

        public int TattooArtistId { get; set; }

        public TattooArtist TattooArtist { get; set; } = null!;
    }
}
