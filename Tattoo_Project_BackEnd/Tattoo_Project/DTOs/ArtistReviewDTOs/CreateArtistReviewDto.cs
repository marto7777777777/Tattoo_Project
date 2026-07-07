namespace Tattoo_Project.DTOs.ArtistReviewDTOs
{
    public class CreateArtistReviewDto
    {
        public int TattooRequestId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}
