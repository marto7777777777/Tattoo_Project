namespace Tattoo_Project.Models
{
    public class ArtistResponse
    {
        public int Id { get; set; }

        public int TattooRequestId { get; set; }

        public TattooRequest TattooRequest { get; set; } = null!;

        public decimal EstimatedPrice { get; set; }

        public int EstimatedHours { get; set; }

        public string ResponseMessage { get; set; } = null!;

        public DateTime CreatedOn { get; set; }
    }
}
