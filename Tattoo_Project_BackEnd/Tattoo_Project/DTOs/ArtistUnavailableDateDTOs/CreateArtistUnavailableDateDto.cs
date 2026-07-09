namespace Tattoo_Project.DTOs.ArtistUnavailableDateDTOs
{
    public class CreateArtistUnavailableDateDto
    {
        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string? Reason { get; set; }
    }
}