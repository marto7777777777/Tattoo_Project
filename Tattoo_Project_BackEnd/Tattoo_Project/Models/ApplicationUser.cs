using Microsoft.AspNetCore.Identity;

namespace Tattoo_Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string? ProfileImageUrl { get; set; }
    }
}