using Microsoft.AspNetCore.Identity;

namespace Tattoo_Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string? ProfileImageUrl { get; set; }
        public DateTime TermsAcceptedAt { get; set; }
        public string TermsVersion { get; set; } = null!;
        public DateTime PrivacyAcceptedAt { get; set; }
        public string PrivacyVersion { get; set; } = null!;
    }
}
