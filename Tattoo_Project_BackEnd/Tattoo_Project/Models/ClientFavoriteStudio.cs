namespace Tattoo_Project.Models
{
    public class ClientFavoriteStudio
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;
        public int StudioId { get; set; }
        public Studio Studio { get; set; } = null!;
    }
}
