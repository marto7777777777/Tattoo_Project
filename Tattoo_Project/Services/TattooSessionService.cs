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
        public async Task<bool> AddMoreSessionsAsync(int tattooRequestId, AddAdditionalSessionsDto dto)
        {
            var request = await context.TattooRequests.FirstOrDefaultAsync(x => x.Id == tattooRequestId);

            if (request is null)
            {
                return false;
            }

            if (request.Status != RequestStatus.ConsultationCompleted &&
                request.Status != RequestStatus.InProgress &&
                request.Status != RequestStatus.TattooBooked)
            {
                return false;
            }

            request.RemainingSessionsToBook += dto.AdditionalSessions;
            foreach (var session in dto.PriceForSession)
            {
                request.PriceForSession.Add(session);
            }
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteTattooAsync(int tattooRequestId)
        {
            var request = await context.TattooRequests.FirstOrDefaultAsync(x => x.Id == tattooRequestId);

            if (request is null)
            {
                return false;
            }

            var hasSessions = await context.TattooSessions
                .AnyAsync(x => x.TattooRequestId == tattooRequestId);

            if (!hasSessions)
            {
                return false; //Трябва да има поне една сесия за да се приключи проекта.
            }

            if (request.Status != RequestStatus.TattooBooked && request.Status != RequestStatus.InProgress)
            {
                return false;
            }

            request.Status = RequestStatus.Completed;

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

        public async Task<bool> CreateTattooSessionAsync(CreateTattooSessionDto dto)
        {
            // 1. Проверка дали заявката съществува
            var tattooRequest = await context.TattooRequests
                .Include(x => x.TattooSessions)
                .FirstOrDefaultAsync(tr => tr.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

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

            //5.Проверка за конфликт с консултации на същия татуйст
            var hasConsultationConflict = await context.Consultations
                .AnyAsync(c =>
            c.TattooRequest.TattooArtistId == tattooArtistId &&
            dto.StartTime < c.EndTime &&
            dto.EndTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return false;
            }

            //6.Няма статус ConsultationCompleted или InProgress
            if (tattooRequest.Status != RequestStatus.ConsultationCompleted &&
                tattooRequest.Status != RequestStatus.InProgress &&
                tattooRequest.Status != RequestStatus.TattooBooked)
            {
                return false;
            }

            //7.Има ли останали RemainingSessionsToBook
            if (tattooRequest.RemainingSessionsToBook <= 0)
            {
                return false;
            }

            var numberSessionInProject = tattooRequest.TattooSessions.Count;
            Console.WriteLine($"Count = {numberSessionInProject}");
            Console.WriteLine($"Prices count = {tattooRequest.PriceForSession.Count}");
            await context.TattooSessions.AddAsync(new TattooSession
            {
                StartTime = dto.StartTime,
                DurationHours = dto.DurationHours,
                EndTime = dto.EndTime,
                PriceForTheSession = tattooRequest.PriceForSession[numberSessionInProject],
                TattooRequestId = dto.TattooRequestId
            });

            tattooRequest.Status = RequestStatus.TattooBooked;

            tattooRequest.RemainingSessionsToBook--;

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
