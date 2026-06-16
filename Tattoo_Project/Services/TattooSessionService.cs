using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooSessionDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class TattooSessionService(TattooDbContext context)
        : ITattooSessionService
    {
        public async Task<ICollection<GetTattooSessionDto>> GetAllTattooSessionsAsync()
        {
            return await context.TattooSessions
                .Select(s => new GetTattooSessionDto
                {
                    TattooRequestId = s.TattooRequestId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    PriceForTheSession = s.PriceForTheSession
                })
                .ToListAsync();
        }

        public async Task<GetTattooSessionDto?> GetTattooSessionByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return null;
            }

            if (isAdmin)
            {
                return MapToDto(tattooSession);
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    tattooSession.TattooRequest.ClientId == client.Id)
                {
                    return MapToDto(tattooSession);
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    tattooSession.TattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return MapToDto(tattooSession);
                }
            }

            return null;
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

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                dto.EndTime > s.StartTime);

            if (hasTattooSessionConflict)
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

        public async Task<bool> UpdateTattooSessionAsync(
            int id,
            UpdateTattooSessionDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return false;
            }

            if (tattooSession.TattooRequest.ClientId != client.Id)
            {
                return false;
            }

            if (tattooSession.StartTime <= DateTime.UtcNow.AddHours(24))
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

            if (tattooSession.TattooRequest.Status != RequestStatus.TattooBooked &&
                tattooSession.TattooRequest.Status != RequestStatus.InProgress)
            {
                return false;
            }

            var tattooArtistId = tattooSession.TattooRequest.TattooArtistId;

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.Id != id &&
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                dto.EndTime > s.StartTime);

            if (hasTattooSessionConflict)
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

            tattooSession.StartTime = dto.StartTime;
            tattooSession.EndTime = dto.EndTime;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTattooSessionAsync(
            int id,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            var tattooSession = await context.TattooSessions
                .Include(s => s.TattooRequest)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (tattooSession == null)
            {
                return false;
            }

            if (tattooSession.TattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return false;
            }

            context.TattooSessions.Remove(tattooSession);

            await context.SaveChangesAsync();

            return true;
        }

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
            var tattooRequest = await context.TattooRequests
                .FirstOrDefaultAsync(r => r.Id == tattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.Completed)
            {
                return false;
            }

            tattooRequest.Status = RequestStatus.InProgress;

            await context.SaveChangesAsync();

            return true;
        }

        private static GetTattooSessionDto MapToDto(TattooSession tattooSession)
        {
            return new GetTattooSessionDto
            {
                TattooRequestId = tattooSession.TattooRequestId,
                StartTime = tattooSession.StartTime,
                EndTime = tattooSession.EndTime,
                PriceForTheSession = tattooSession.PriceForTheSession
            };
        }
    }
}