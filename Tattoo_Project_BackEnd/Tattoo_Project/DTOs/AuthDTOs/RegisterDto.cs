namespace Tattoo_Project.DTOs.AuthDTOs
{
    public class RegisterDto
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
        public bool AcceptTermsAndPrivacy { get; set; }
        public string TermsVersion { get; set; } = null!;
        public string PrivacyVersion { get; set; } = null!;
    }
}
