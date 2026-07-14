namespace Tattoo_Project.Models;

public class AiTattooProject
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string TattooStyle { get; set; } = null!;
    public string Placement { get; set; } = null!;
    public string InitialDescription { get; set; } = null!;
    public string? InitialReferenceImageUrl { get; set; }
    public bool IsFreeProject { get; set; }
    public int FreeEditsUsed { get; set; }
    public DateTime? EditingAccessUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<AiTattooVersion> Versions { get; set; } = new List<AiTattooVersion>();
    public ICollection<AiProjectPayment> Payments { get; set; } = new List<AiProjectPayment>();
}
