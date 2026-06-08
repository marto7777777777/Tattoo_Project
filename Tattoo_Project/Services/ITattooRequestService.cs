using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.DTOs.TattooRequestDTOs;

namespace Tattoo_Project.Services
{
    public interface ITattooRequestService
    {
        Task<List<GetTattooRequestDto>> GetAllTattooRequestsAsync();
        Task<GetTattooRequestDto> GetTattooRequestByIdAsync(int id);
        Task<bool> CreateTattooRequest(CreateTattooRequestDto dto);
        Task<bool> UpdateTattooRequest(int id, UpdateTattooRequestDto dto);
        Task<bool> DeleteTattooRequest(int id);
    }
}
