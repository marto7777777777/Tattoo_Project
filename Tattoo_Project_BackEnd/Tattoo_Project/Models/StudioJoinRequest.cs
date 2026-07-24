namespace Tattoo_Project.Models
{
    public class StudioJoinRequest
    {
        public int Id { get; set; }
        public int StudioId { get; set; }
        public Studio Studio { get; set; } = null!;
        public int TattooArtistId { get; set; }
        public TattooArtist TattooArtist { get; set; } = null!;
        public StudioJoinRequestStatus Status { get; set; } = StudioJoinRequestStatus.Pending;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedOn { get; set; }
    }
}
