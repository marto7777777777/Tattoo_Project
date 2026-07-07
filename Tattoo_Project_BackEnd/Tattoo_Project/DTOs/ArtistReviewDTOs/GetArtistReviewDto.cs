namespace Tattoo_Project.DTOs.ArtistReviewDTOs
{
    public class GetArtistReviewDto
    {
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
