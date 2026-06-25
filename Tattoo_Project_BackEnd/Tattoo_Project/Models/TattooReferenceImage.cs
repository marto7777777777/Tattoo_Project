namespace Tattoo_Project.Models
{
    public class TattooReferenceImage
    {
        public int Id { get; set; }

        public string ImageUrl { get; set; } = null!;

        public int TattooRequestId { get; set; }

        public TattooRequest TattooRequest { get; set; } = null!;
    }
}
