using Tattoo_Project.DTOs.TattooReferenceImageDTOs;

namespace Tattoo_Project.DTOs.TattooRequestDTOs
{
    public class UpdateTattooRequestDto
    {
        public ICollection<TattooReferenceImageDto> Images { get; set; } = new List<TattooReferenceImageDto>();
        public string Description { get; set; } = null!;
    }
}
