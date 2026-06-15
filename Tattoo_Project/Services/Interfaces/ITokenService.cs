using Tattoo_Project.Models;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
    }
}
