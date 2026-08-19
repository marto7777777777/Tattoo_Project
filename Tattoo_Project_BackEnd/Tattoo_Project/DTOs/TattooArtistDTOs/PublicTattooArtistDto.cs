namespace Tattoo_Project.DTOs.TattooArtistDTOs
{
    // Deliberately contains public presentation data only. The phone number is
    // included only when the artist has explicitly enabled its public visibility.
    public class PublicTattooArtistDto
    {
        public int Id { get; set; }
        public string PublicProfileSlug { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? ProfileImageUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string Description { get; set; } = null!;
        public bool IsVerified { get; set; }
        public string StudioName { get; set; } = null!;
        public string StudioAddress { get; set; } = null!;
        public string StudioCity { get; set; } = null!;
        public string StudioCountry { get; set; } = null!;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public ICollection<string> SpecialtyStyles { get; set; } = new List<string>();
        public ICollection<TattooArtistPortfolioImageDto> PortfolioImages { get; set; } = new List<TattooArtistPortfolioImageDto>();
        public ICollection<TattooArtistRequirementsDto> Requirements { get; set; } = new List<TattooArtistRequirementsDto>();
    }
}
