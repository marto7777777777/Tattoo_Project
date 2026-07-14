namespace Tattoo_Project.Models;

public class AiTattooVersion
{
    public int Id { get; set; }
    public int AiTattooProjectId { get; set; }
    public AiTattooProject AiTattooProject { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string Prompt { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public int? ParentVersionId { get; set; }
    public AiTattooVersion? ParentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
}
