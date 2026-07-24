namespace Tattoo_Project.Models
{
    public class Studio
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsOpenForJoinRequests { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Nullable at database level to make studio + owner creation safe in one workflow.
        // Application validation always assigns an owner before the create workflow completes.
        public int? OwnerArtistId { get; set; }
        public TattooArtist? OwnerArtist { get; set; }

        public ICollection<TattooArtist> Artists { get; set; } = new List<TattooArtist>();
        public ICollection<StudioJoinRequest> JoinRequests { get; set; } = new List<StudioJoinRequest>();
    }
}
