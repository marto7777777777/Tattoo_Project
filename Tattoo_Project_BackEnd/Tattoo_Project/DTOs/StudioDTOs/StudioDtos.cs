using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.StudioDTOs
{
    public class CreateStudioDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class UpdateStudioDto : CreateStudioDto
    {
    }

    public class StudioArtistDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? ProfileImageUrl { get; set; }
        public string Description { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public bool IsVerified { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime? JoinedStudioOn { get; set; }
        public ICollection<string> PortfolioImageUrls { get; set; } = new List<string>();
    }

    public class StudioDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsOpenForJoinRequests { get; set; }
        public int ArtistCount { get; set; }
        public ICollection<StudioArtistDto> Artists { get; set; } = new List<StudioArtistDto>();
        public ICollection<string> PortfolioPreviewUrls { get; set; } = new List<string>();
    }

    public class StudioJoinRequestDto
    {
        public int Id { get; set; }
        public int StudioId { get; set; }
        public string StudioName { get; set; } = null!;
        public int TattooArtistId { get; set; }
        public string ArtistName { get; set; } = null!;
        public string ArtistDescription { get; set; } = null!;
        public string ArtistPhoneNumber { get; set; } = null!;
        public string? ArtistProfileImageUrl { get; set; }
        public StudioJoinRequestStatus Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? RespondedOn { get; set; }
    }

    public class MyStudioDto
    {
        public int CurrentArtistId { get; set; }
        public bool HasStudio { get; set; }
        public bool IsOwner { get; set; }
        public StudioDto? Studio { get; set; }
        public StudioJoinRequestDto? PendingJoinRequest { get; set; }
        public StudioJoinRequestDto? LatestJoinRequest { get; set; }
        public ICollection<StudioJoinRequestDto> PendingRequests { get; set; } = new List<StudioJoinRequestDto>();
    }

    public class UpdateStudioOpenStateDto
    {
        public bool IsOpen { get; set; }
    }
}
