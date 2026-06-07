using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs;

namespace Tattoo_Project.Services
{
    public interface IClientService
    {
        Task<List<GetClientDto>> GetAllClientsAsync();
        Task<GetClientDto> GetClientsByIdAsync(int id);
        Task<bool> CreateClient(CreateClientDto dto);
        Task<bool> DeleteClient(int id);
        Task<bool> UpdateClient(int id, UpdateClientDto dto);
    }
}
