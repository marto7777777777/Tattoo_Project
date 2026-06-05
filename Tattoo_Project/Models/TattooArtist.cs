namespace Tattoo_Project.Models
{
    public class TattooArtist
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public string StudioName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string StudioAddress { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public bool IsVerified { get; set; }

        public bool OffersOnlineConsultation { get; set; }

        public bool RequiresDeposit { get; set; }

        public decimal? DepositAmount { get; set; }
        public ICollection<ArtistRequirement> Requirements { get; set; }
    = new List<ArtistRequirement>();
        public ICollection<PortfolioImage> PortfolioImages { get; set; }
    = new List<PortfolioImage>();

        public ICollection<TattooRequest> TattooRequests { get; set; }
            = new List<TattooRequest>();

        public ICollection<Schedule> Schedules { get; set; } 
            = new List<Schedule>();
    }
}
