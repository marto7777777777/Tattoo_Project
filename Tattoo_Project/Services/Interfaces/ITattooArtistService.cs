using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs.TattooArtistDTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services.Interfaces
{
    public interface ITattooArtistService
    {
        Task<List<GetTattooArtistDto>> GetAllArtistsAsync();
        Task<GetTattooArtistDto> GetTattooArtistByIdAsync(int Id);
        Task<bool> CreateTattooArtistProfileAsync(CreateTattooArtistDto dto, string userId);
        Task<bool> DeleteArtist(int id);
        Task<bool> UpdateArtist(int id, UpdateArtistDto dto);
    }
}
