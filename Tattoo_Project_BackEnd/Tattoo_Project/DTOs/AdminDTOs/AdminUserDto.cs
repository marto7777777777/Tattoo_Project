namespace Tattoo_Project.DTOs.AdminDTOs;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public ICollection<string> Roles { get; set; } = new List<string>();
    public int? ClientProfileId { get; set; }
    public int? ArtistProfileId { get; set; }
    public int TattooRequestCount { get; set; }
    public int AiProjectCount { get; set; }
}
