using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.ArtistResponseDTOs
{
    public class GetArtistResponseDto
    {

        public int TattooRequestId { get; set; }

        public decimal EstimatedPrice { get; set; }

        public int EstimatedHours { get; set; }

        public string ResponseMessage { get; set; } = null!;

        public DateTime CreatedOn { get; set; }
    }
}
