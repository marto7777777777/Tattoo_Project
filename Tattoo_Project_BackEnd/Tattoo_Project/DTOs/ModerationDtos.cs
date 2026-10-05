namespace Tattoo_Project.DTOs;

public sealed class CreateArtistReportDto
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class ArtistModerationStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? PendingSubmissionId { get; set; }
    public ICollection<ModerationImageDto> Images { get; set; } = new List<ModerationImageDto>();
}

public sealed class ModerationImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
