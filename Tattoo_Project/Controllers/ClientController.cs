using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.ClientDTOs;
using Tattoo_Project.Services;

namespace Tattoo_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController(IClientService service) : ControllerBase
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

        [HttpPost]
        public async Task<IActionResult> CreateClientAsync(CreateClientDto dto)
        {
            var isCreated = await service.CreateClient(dto);
            
            return Ok(isCreated);
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
