namespace Tattoo_Project.Models
{
    public class ClientFavoriteArtist
    {
        public int Id { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public int ClientId { get; set; }

        public Client Client { get; set; } = null!;

        public int TattooArtistId { get; set; }

        public TattooArtist TattooArtist { get; set; } = null!;
    }
}