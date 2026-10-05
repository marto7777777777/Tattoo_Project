namespace Tattoo_Project.Models;

public class ArtistReport
{
    public int Id { get; set; }
    public int ReportedArtistId { get; set; }
    public TattooArtist ReportedArtist { get; set; } = null!;
    public string ReporterUserId { get; set; } = null!;
    public ApplicationUser ReporterUser { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public ArtistReportStatus Status { get; set; } = ArtistReportStatus.Pending;
    public string? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
