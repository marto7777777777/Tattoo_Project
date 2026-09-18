namespace Tattoo_Project.Models;

public class LegalConsentAudit
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string TermsVersion { get; set; } = null!;
    public string PrivacyVersion { get; set; } = null!;
    public DateTime AcceptedAt { get; set; }
}
