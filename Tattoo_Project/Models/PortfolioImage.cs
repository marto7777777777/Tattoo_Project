namespace Tattoo_Project.Models
{
    public class PortfolioImage
    {
        public int Id { get; set; }

        public string ImageUrl { get; set; } = null!;

        public int TattooArtistId { get; set; }

        public TattooArtist TattooArtist { get; set; } = null!;
    }
}
