namespace Tattoo_Project.Models;

public enum ArtistModerationSubmissionStatus
{
    Pending = 0,
    Rejected = 1,
    Approved = 2
}

public class ArtistModerationSubmission
{
    public int Id { get; set; }
    public int TattooArtistId { get; set; }
    public TattooArtist TattooArtist { get; set; } = null!;
    public int? SourceReportId { get; set; }
    public ArtistReport? SourceReport { get; set; }
    public ArtistModerationSubmissionStatus Status { get; set; } = ArtistModerationSubmissionStatus.Draft;
    public string? AdminNote { get; set; }
    public string? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public ICollection<ArtistModerationSubmissionImage> Images { get; set; } = new List<ArtistModerationSubmissionImage>();
}

public class ArtistModerationSubmissionImage
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public ArtistModerationSubmission Submission { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
}
