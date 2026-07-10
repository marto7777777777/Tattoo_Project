namespace Tattoo_Project.Models
{
    public class EmailVerificationCode
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public string CodeHash { get; set; } = null!;

        public EmailVerificationPurpose Purpose { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }
    }
}
