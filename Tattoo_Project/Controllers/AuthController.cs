using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.Models;
using Microsoft.AspNetCore.Authorization;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
        : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Role != UserRoles.Client &&
                dto.Role != UserRoles.TattooArtist)
            {
                return BadRequest("Invalid role.");
            }

            var existingUserName = await userManager.FindByNameAsync(dto.UserName);

            if (existingUserName != null)
            {
                return BadRequest("Username is already taken.");
            }

            var existingEmail = await userManager.FindByEmailAsync(dto.Email);

            if (existingEmail != null)
            {
                return BadRequest("Email is already registered.");
            }

            if (!await roleManager.RoleExistsAsync(dto.Role))
            {
                await roleManager.CreateAsync(new IdentityRole(dto.Role));
            }

            ApplicationUser user = new()
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.UserName,
                Email = dto.Email
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await userManager.AddToRoleAsync(user, dto.Role);

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Login)
                       ?? await userManager.FindByNameAsync(dto.Login);

            if (user == null)
            {
                return Unauthorized("Invalid login or password.");
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordValid)
            {
                return Unauthorized("Invalid login or password.");
            }

            var roles = await userManager.GetRolesAsync(user);

            var token = GenerateJwtToken(user, roles);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.UserName,
                    user.Email,
                    Roles = roles
                }
            });
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expiresInMinutes = int.Parse(
                configuration["Jwt:ExpiresInMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}