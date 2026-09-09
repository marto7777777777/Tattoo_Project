namespace Tattoo_Project.Models;

public static class AnalyticsMilestones
{
    public const string FirstProjectCompleted = "first_project_completed";
    public const string TenthProjectCompleted = "tenth_project_completed";
    public const string FirstSubscriptionPayment = "subscription_first_payment_succeeded";
    public const string SecondSubscriptionPayment = "subscription_second_payment_succeeded";
}

public class ArtistAnalyticsMilestone
{
    public long Id { get; set; }
    public int TattooArtistId { get; set; }
    public TattooArtist TattooArtist { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}
