using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class TattooSessionService(TattooDbContext context) : ITattooSessionService
    {
        public async Task<bool> AddMoreSessionsAsync(
        int tattooRequestId,
        AddAdditionalSessionsDto dto,
        string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.TattooBooked &&
                tattooRequest.Status != RequestStatus.InProgress &&
                tattooRequest.Status != RequestStatus.ConsultationCompleted)
            {
                return false;
            }

            if (dto.AdditionalSessions <= 0)
            {
                return false;
            }

            if (dto.PriceForSession == null ||
                dto.PriceForSession.Count != dto.AdditionalSessions)
            {
                return false;
            }

            if (dto.PriceForSession.Any(price => price <= 0))
            {
                return false;
            }


            tattooRequest.PriceForSession ??= new List<decimal>();

            tattooRequest.RemainingSessionsToBook ??= 0;

            tattooRequest.RemainingSessionsToBook += dto.AdditionalSessions;

            foreach (var price in dto.PriceForSession)
            {
                tattooRequest.PriceForSession.Add(price);
            }

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteTattooAsync(
    int tattooRequestId,
    string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.TattooBooked &&
                tattooRequest.Status != RequestStatus.InProgress)
            {
                return false;
            }

            var hasTattooSessions = await context.TattooSessions
                .AnyAsync(s => s.TattooRequestId == tattooRequestId);

            if (!hasTattooSessions)
            {
                return false;
            }

            if (tattooRequest.RemainingSessionsToBook != null &&
                tattooRequest.RemainingSessionsToBook > 0)
            {
                return false;
            }

            tattooRequest.Status = RequestStatus.Completed;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ContinueTattooAsync(int tattooRequestId)
        {
            var request = await context.TattooRequests
                .FirstOrDefaultAsync(x => x.Id == tattooRequestId);

            if (request is null)
            {
                return false;
            }

            var hasSession = await context.TattooSessions
                .AnyAsync(x => x.TattooRequestId == tattooRequestId);

            if (!hasSession)
            {
                return false; //Трябва да има поне една сесия за да продължът към следващите.
            }

            if (request.Status != RequestStatus.TattooBooked && request.Status != RequestStatus.InProgress)
            {
                return false;
            }

            request.Status = RequestStatus.InProgress;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CreateTattooSessionAsync(
    CreateTattooSessionDto dto,
    string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return false;
            }

            if (dto.StartTime >= dto.EndTime)
            {
                return false;
            }

            if (dto.StartTime <= DateTime.UtcNow)
            {
                return false;
            }

            if (dto.EndTime - dto.StartTime < TimeSpan.FromMinutes(15))
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.ConsultationCompleted &&
                tattooRequest.Status != RequestStatus.TattooBooked &&
                tattooRequest.Status != RequestStatus.InProgress)
            {
                return false;
            }

            if (tattooRequest.RemainingSessionsToBook == null ||
                tattooRequest.RemainingSessionsToBook <= 0)
            {
                return false;
            }

            if (tattooRequest.PriceForSession == null ||
                !tattooRequest.PriceForSession.Any())
            {
                return false;
            }

            var existingSessionsCount = await context.TattooSessions
                .CountAsync(s => s.TattooRequestId == dto.TattooRequestId);

            if (existingSessionsCount >= tattooRequest.PriceForSession.Count)
            {
                return false;
            }

            var tattooArtistId = tattooRequest.TattooArtistId;

            var hasSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                dto.EndTime > s.StartTime);

            if (hasSessionConflict)
            {
                return false;
            }

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                dto.EndTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return false;
            }

            var price = tattooRequest.PriceForSession[existingSessionsCount];

            TattooSession tattooSession = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                PriceForTheSession = price
            };

            context.TattooSessions.Add(tattooSession);

            tattooRequest.RemainingSessionsToBook--;

            tattooRequest.Status = RequestStatus.TattooBooked;

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
                PriceForTheSession = x.PriceForTheSession,
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
                PriceForTheSession = tattooSession.PriceForTheSession,
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
