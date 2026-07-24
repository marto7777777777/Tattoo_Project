namespace Tattoo_Project.Models
{
    public class TattooArtist
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public int? StudioId { get; set; }
        public Studio? Studio { get; set; }
        public DateTime? JoinedStudioOn { get; set; }

        public bool IsVerified { get; set; }

        public bool OffersOnlineConsultation { get; set; }

        public bool RequiresDeposit { get; set; }

        public decimal? DepositAmount { get; set; }

        public string UserId { get; set; } = null!;

        public int ConsultationDurationMinutes { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public ICollection<ArtistRequirement> Requirements { get; set; }
    = new List<ArtistRequirement>();
        public ICollection<PortfolioImage> PortfolioImages { get; set; }
    = new List<PortfolioImage>();

        public ICollection<TattooRequest>? TattooRequests { get; set; }
            = new List<TattooRequest>();

        public ICollection<Schedule> Schedules { get; set; } 
            = new List<Schedule>();

        public ICollection<ArtistReview> Reviews { get; set; }
            = new List<ArtistReview>();

        public ICollection<ArtistUnavailableDate> UnavailableDates { get; set; }
        = new List<ArtistUnavailableDate>();

        public ICollection<StudioJoinRequest> StudioJoinRequests { get; set; }
            = new List<StudioJoinRequest>();
    }
}
