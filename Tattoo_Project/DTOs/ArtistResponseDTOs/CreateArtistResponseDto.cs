namespace Tattoo_Project.DTOs.ArtistResponseDTOs
{
    public class CreateArtistResponseDto
    {
        public int TattooRequestId { get; set; }

        public decimal EstimatedPrice { get; set; }

        public int EstimatedHours { get; set; }

        public string ResponseMessage { get; set; } = null!;
    }
}
