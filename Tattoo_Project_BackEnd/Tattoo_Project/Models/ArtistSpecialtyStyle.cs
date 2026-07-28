namespace Tattoo_Project.Models
{
    public class ArtistSpecialtyStyle
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int TattooArtistId { get; set; }
        public TattooArtist TattooArtist { get; set; } = null!;
    }
}
