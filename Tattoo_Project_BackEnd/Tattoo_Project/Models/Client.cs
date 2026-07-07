namespace Tattoo_Project.Models
{
    public class Client
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Country { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<TattooRequest>? TattooRequests { get; set; }
            = new List<TattooRequest>();

        public ICollection<ArtistReview> ArtistReviews { get; set; }
            = new List<ArtistReview>();
    }
}
