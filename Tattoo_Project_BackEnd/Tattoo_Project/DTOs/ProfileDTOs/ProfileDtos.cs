namespace Tattoo_Project.DTOs.ProfileDTOs
{
    public class CurrentProfileDto
    {
        public bool IsTattooArtist { get; set; }
        public bool IsClient { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ProfileImageUrl { get; set; }

        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public ArtistProfileSectionDto? Artist { get; set; }
    }

    public class ArtistProfileSectionDto
    {
        public string StudioName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string StudioAddress { get; set; } = null!;
        public string StudioCity { get; set; } = null!;
        public string StudioCountry { get; set; } = null!;
        public int ConsultationDurationMinutes { get; set; }
        public bool OffersOnlineConsultation { get; set; }
        public bool RequiresDeposit { get; set; }
        public decimal? DepositAmount { get; set; }
        public ICollection<ProfileRequirementDto> Requirements { get; set; } = new List<ProfileRequirementDto>();
        public ICollection<ProfilePortfolioImageDto> PortfolioImages { get; set; } = new List<ProfilePortfolioImageDto>();
    }

    public class ProfileRequirementDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = null!;
    }

    public class ProfilePortfolioImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = null!;
    }

    public class UpdateStringValueDto
    {
        public string Value { get; set; } = null!;
    }

    public class UpdateNullableDecimalValueDto
    {
        public decimal? Value { get; set; }
    }

    public class UpdateIntValueDto
    {
        public int Value { get; set; }
    }

    public class UpdateBoolValueDto
    {
        public bool Value { get; set; }
    }
}
