using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tattoo_Project.DTOs.AuthDTOs;
using Tattoo_Project.DTOs.ClientDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services;
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
        
        [HttpGet]
        public async Task<ActionResult<GetClientDto>> GetAllClients()
        {
            var clients = await service.GetAllClientsAsync();
            if (clients is null || !clients.Any())
            {
                return NotFound("No clients yet!");
            }
            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetClientDto>> GetClientById(int id)
        {
            var client = await service.GetClientsByIdAsync(id);
            if (client == null)
            {
                return NotFound($"Client with id {id} doesn't exist!");
            }
            return Ok(client);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClientAsync(int id)
        {
            var isDeleted = await service.DeleteClient(id);
            if (isDeleted == false)
            {
                return NotFound($"Client with id {id} already doesn't exist!");
            }
            return Ok(isDeleted);
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

            var result = await service.CreateClientProfileAsync(dto, userId);

            if (!result)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClientAsync(int id, UpdateClientDto dto)
        {
            var isUpdated = await service.UpdateClient(id, dto);
            if (isUpdated == false)
            {
                return NotFound($"Client with id {id} doesn't exist!");
            }
            return Ok(isUpdated);
        }
    }
}
