using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs
{
    public class ClientTattooRequestsDto
    {
        public string Description { get; set; } = null!;
        public string Placement { get; set; } = null!;
        public DateTime CreatedOn { get; set; }
        public RequestStatus Status { get; set; }
        public ConsultationDto? Consultation { get; set; }
        public ICollection<TattooSessionDto>? TattooSessions { get; set; } = new List<TattooSessionDto>();
        public ICollection<TattooReferenceImageDto> Images { get; set; } = new List<TattooReferenceImageDto>();
        public ArtistResponseDto ArtistResponse { get; set; }
    }
}
