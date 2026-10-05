namespace Tattoo_Project.DTOs.AdminDTOs;

public sealed class AdminArtistReportDto
{
    public int Id { get; set; }
    public int ReportedArtistId { get; set; }
    public string ReportedArtistName { get; set; } = string.Empty;
    public string? ReportedArtistEmail { get; set; }
    public string PublicProfileSlug { get; set; } = string.Empty;
    public string ReporterUserId { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DecisionNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class AdminModerationSubmissionDto
{
    public int Id { get; set; }
    public int TattooArtistId { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public string ArtistEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? SourceReportId { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public ICollection<string> ImageUrls { get; set; } = new List<string>();
}

public sealed class ModerateArtistReportRequest
{
    public bool BlockArtist { get; set; }
    public string? DecisionNote { get; set; }
}

public sealed class ModerateArtistSubmissionRequest
{
    public bool Approve { get; set; }
    public string? AdminNote { get; set; }
}
