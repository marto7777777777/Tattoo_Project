namespace Tattoo_Project.DTOs.ArtistUnavailableDateDTOs
{
    public class GetArtistUnavailableDateDto
    {
        public int Id { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }
    }
}