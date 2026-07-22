namespace Tattoo_Project.DTOs.AdminDTOs;

public class AdminAiProjectDto
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TattooStyle { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
