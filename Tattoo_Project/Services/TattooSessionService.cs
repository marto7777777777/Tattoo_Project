using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class TattooSessionService(TattooDbContext context) : ITattooSessionService
    {
        public async Task<bool> CreateTattooSessionAsync(CreateTattooSessionDto dto)
        {
            // 1. Проверка дали заявката съществува
            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(tr => tr.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            
            //if (tattooRequest.Status != RequestStatus.ConsultationCompleted)
            //{
              //  return false; // Не е свършила консултацията или татуйста не е дал права на клиента за да запази тату сесия.
            //}

            // 2. Валиден интервал (Start < End)
            if (dto.StartTime >= dto.EndTime)
            {
                return false;
            }

            // 3. Да не е в миналото
            if (dto.StartTime < DateTime.UtcNow)
            {
                return false;
            }

            // 4. Проверка за конфликт с други tattoo sessions на същия татуист
            var tattooArtistId = tattooRequest.TattooArtistId;

            var hasConflict = await context.TattooSessions
                .AnyAsync(s =>
                    s.TattooRequest.TattooArtistId == tattooArtistId &&
                    dto.StartTime < s.EndTime &&
                    dto.EndTime > s.StartTime);

            if (hasConflict)
            {
                return false;
            }

            await context.TattooSessions.AddAsync(new TattooSession
            {
                StartTime = dto.StartTime,
                DurationHours = dto.DurationHours,
                EndTime = dto.EndTime,
                FinalPrice = dto.FinalPrice,
                TattooRequestId = dto.TattooRequestId
            });

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTattooSessionAsync(int id)
        {
            var tattooSession = await context.TattooSessions.FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return false; // Вече няма такава сесия!
            }

            if (tattooSession.StartTime <= DateTime.UtcNow.AddHours(24))
            {
                return false; // Остават по малко от 24 часа
            }

            context.TattooSessions.Remove(tattooSession);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GetTattooSessionDto>> GetAllTattooSessionsAsync()
        {

            return !context.TattooSessions.Any() || context.TattooSessions is null ? null 
                : await context.TattooSessions.Select(x => new GetTattooSessionDto
            {
                StartTime = x.StartTime,
                DurationHours = x.DurationHours,
                EndTime = x.EndTime,
                FinalPrice = x.FinalPrice,
                TattooRequestId = x.TattooRequestId
            }).ToListAsync();

        }
        public async Task<GetTattooSessionDto> GetTattooSessionByIdAsync(int id)
        {
            var tattooSession = await context.TattooSessions.FirstOrDefaultAsync(x => x.Id == id);

            if (tattooSession == null)
            {
                return null;
            }

            var tattooSessionDto = new GetTattooSessionDto
            {
                StartTime = tattooSession.StartTime,
                DurationHours = tattooSession.DurationHours,
                EndTime = tattooSession.EndTime,
                FinalPrice = tattooSession.FinalPrice,
                TattooRequestId = tattooSession.TattooRequestId
            };
            return tattooSessionDto;
        }

        public async Task<bool> UpdateTattooSessionAsync(int id, UpdateTattooSessionDto dto)
        {
            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(x => x.Id == id);

            // 2. Има ли такава тату сесия изобщо
            if (tattooSession == null)
            {
                return false;
            }

            // 2. Не може да се редактира ако остават < 24 часа
            if (tattooSession.StartTime - DateTime.UtcNow < TimeSpan.FromHours(24))
            {
                return false;
            }

            // 3. Валиден интервал
            if (dto.StartTime >= dto.EndTime)
            {
                return false;
            }

            // 4. Да не е в миналото
            if (dto.StartTime < DateTime.UtcNow)
            {
                return false;
            }

            // 5. Проверка за конфликт с други сесии на същия татуист
            var tattooArtistId = tattooSession.TattooRequest.TattooArtistId;

            var hasConflict = await context.TattooSessions
                .AnyAsync(s =>
                    s.Id != tattooSession.Id &&
                    s.TattooRequest.TattooArtistId == tattooArtistId &&
                    dto.StartTime < s.EndTime &&
                    dto.EndTime > s.StartTime);

            if (hasConflict)
            {
                return false;
            }

            // UPDATE
            tattooSession.StartTime = dto.StartTime;
            tattooSession.EndTime = dto.EndTime;
            tattooSession.DurationHours = dto.DurationHours;

            await context.SaveChangesAsync();

            return true;
        }
    }
}
