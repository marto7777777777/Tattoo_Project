namespace Tattoo_Project.Models
{
    public class Client
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public ICollection<TattooRequest> TattooRequests { get; set; }
            = new List<TattooRequest>();
    }
}
