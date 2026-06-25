using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.TattooRequestDTOs
{
    public class CreateTattooRequestDto
    {
        public string Description { get; set; } = null!;
        public string Placement { get; set; } = null!;
        public int TattooArtistId { get; set; }
        public ICollection<TattooReferenceImageDto> Images { get; set; } = new List<TattooReferenceImageDto>();
    }
}
