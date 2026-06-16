using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.ClientDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController(
        IClientService service,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
        : ControllerBase
    {
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllClients()
        {
            var clients = await service.GetAllClientsAsync();

            return Ok(clients);
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientById(int id)
        {
            var client = await service.GetClientByIdAsync(id);

            if (client == null)
            {
                return NotFound("Client not found.");
            }

            return Ok(client);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("profile")]
        public async Task<IActionResult> CreateClientProfile(CreateClientDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isCreated = await service.CreateClientProfileAsync(dto, userId);

            if (!isCreated)
            {
                return BadRequest("Client profile already exists or invalid data.");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var token = await tokenService.GenerateJwtTokenAsync(user);

            return Ok(new
            {
                Message = "Client profile created successfully.",
                Token = token
            });
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Client)]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateClientProfile(UpdateClientDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var isUpdated = await service.UpdateClientProfileAsync(dto, userId);

            if (!isUpdated)
            {
                return BadRequest("Client profile could not be updated.");
            }

            return Ok("Client profile updated successfully.");
        }

        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = UserRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var isDeleted = await service.DeleteClientAsync(id);

            if (!isDeleted)
            {
                return NotFound("Client not found.");
            }

            return Ok("Client deleted successfully.");
        }
    }
}