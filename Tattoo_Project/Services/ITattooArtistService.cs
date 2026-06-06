using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.DTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services
{
    public interface ITattooArtistService
    {
        Task<List<GetTattooArtistDto>> GetAllArtistsAsync();
        Task<GetTattooArtistDto> GetTattooArtistByIdAsync(int Id);
        Task<int> CreateArtist(CreateTattooArtistDto dto);
        Task<bool> DeleteArtist(int id);
        Task<bool> UpdateArtist(int id, UpdateArtistDto dto);
    }
}
