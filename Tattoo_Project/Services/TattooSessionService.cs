using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class TattooSessionService(TattooDbContext context) : ITattooSessionService
    {
        public Task<bool> CreateTattooSession(CreateTattooSessionDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteTattooSession(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<GetTattooSessionDto>> GetAllTattooSessions()
        {
            throw new NotImplementedException();
        }

        public Task<GetTattooSessionDto> GetTattooSessionById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateTattooSession(int id, UpdateTattooSessionDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
