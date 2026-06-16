using Tattoo_Project.DTOs.ClientDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IClientService
    {
        Task<ICollection<GetClientDto>> GetAllClientsAsync();

        Task<GetClientDto?> GetClientByIdAsync(int id);

        Task<bool> CreateClientProfileAsync(CreateClientDto dto, string userId);

        Task<bool> UpdateClientProfileAsync(UpdateClientDto dto, string userId);

        Task<bool> DeleteClientAsync(int id);
    }
}
