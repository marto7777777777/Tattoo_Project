namespace Tattoo_Project.DTOs.AdminDTOs;

public class AdminTattooRequestDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public string TattooStyle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}
