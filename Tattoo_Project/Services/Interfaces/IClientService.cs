using Tattoo_Project.DTOs.ClientDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IClientService
    {
        Task<ResultService<ICollection<GetClientDto>>> GetAllClientsAsync();

        Task<ResultService<GetClientDto>> GetClientByIdAsync(int id);

        Task<ResultService> CreateClientProfileAsync(
            CreateClientDto dto,
            string userId);

        Task<ResultService> UpdateClientProfileAsync(
            UpdateClientDto dto,
            string userId);

        Task<ResultService> DeleteClientAsync(int id);
    }
}