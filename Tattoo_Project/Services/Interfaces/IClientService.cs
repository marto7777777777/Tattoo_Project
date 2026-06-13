using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.ClientDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IClientService
    {
        Task<List<GetClientDto>> GetAllClientsAsync();
        Task<GetClientDto> GetClientsByIdAsync(int id);
        Task<bool> CreateClientProfileAsync(CreateClientDto dto, string userId);
        Task<bool> DeleteClient(int id);
        Task<bool> UpdateClient(int id, UpdateClientDto dto);
    }
}
