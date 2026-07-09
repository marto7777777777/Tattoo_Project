using Tattoo_Project.DTOs.ArtistResponceDTOs;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.TattooRequestDTOs
{
    public class GetTattooRequestDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public string Placement { get; set; } = null!;
        public DateTime CreatedOn { get; set; }
        public int ClientId { get; set; }
        public int TattooArtistId { get; set; }
        public string? ClientName { get; set; }
        public string? TattooArtistName { get; set; }
        public string? StudioName { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime? UpcomingConsultationStartTime { get; set; }
        public DateTime? UpcomingTattooSessionStartTime { get; set; }
        public ConsultationDto? Consultation { get; set; }
        public ICollection<TattooSessionDto>? TattooSessions { get; set; } = new List<TattooSessionDto>();
        public ICollection<TattooReferenceImageDto> Images { get; set; } = new List<TattooReferenceImageDto>();
        public ArtistResponseDto? ArtistResponse { get; set; }
    }
}
