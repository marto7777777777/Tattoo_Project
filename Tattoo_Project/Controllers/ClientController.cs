using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs;
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
    }
}
