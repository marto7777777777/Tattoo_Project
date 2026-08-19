namespace Tattoo_Project.Models
{
    public class PendingRegistration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string NormalizedUserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string NormalizedEmail { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string VerificationCodeHash { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
