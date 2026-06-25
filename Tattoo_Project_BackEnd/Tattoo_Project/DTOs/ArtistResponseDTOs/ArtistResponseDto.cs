namespace Tattoo_Project.DTOs.ArtistResponceDTOs
{
    public class ArtistResponseDto
    {
        public decimal EstimatedPrice { get; set; }

        public int EstimatedHours { get; set; }

        public string ResponseMessage { get; set; } = null!;

        public DateTime CreatedOn { get; set; }
    }
}
