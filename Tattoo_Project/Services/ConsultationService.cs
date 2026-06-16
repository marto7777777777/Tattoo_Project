using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class ConsultationService(TattooDbContext context)
        : IConsultationService
    {
        public async Task<ICollection<GetConsultationDto>> GetAllConsultationsAsync()
        {
            return await context.Consultations
                .Select(c => new GetConsultationDto
                {
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    IsOnline = c.IsOnline,
                    IsCompleted = c.IsCompleted
                })
                .ToListAsync();
        }

        public async Task<GetConsultationDto?> GetConsultationByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var consultation = await context.Consultations
                .Include(c => c.TattooRequest)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return null;
            }

            if (isAdmin)
            {
                return MapToDto(consultation);
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    consultation.TattooRequest.ClientId == client.Id)
                {
                    return MapToDto(consultation);
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    consultation.TattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return MapToDto(consultation);
                }
            }

            return null;
        }

        public async Task<bool> CreateConsultationAsync(
            CreateConsultationDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.TattooArtist)
                .FirstOrDefaultAsync(r => r.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.WaitingForConsultation)
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

            var alreadyHasConsultation = await context.Consultations
                .AnyAsync(c => c.TattooRequestId == dto.TattooRequestId);

            if (alreadyHasConsultation)
            {
                return false;
            }
            var tattooArtistId = tattooRequest.TattooArtistId;

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                dto.EndTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return false;
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                dto.EndTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return false;
            }

            Consultation consultation = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsCompleted = false
            };

            context.Consultations.Add(consultation);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateConsultationAsync(
            int id,
            UpdateConsultationDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var consultation = await context.Consultations
                .Include(c => c.TattooRequest)
                    .ThenInclude(r => r.TattooArtist)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return false;
            }

            if (consultation.TattooRequest.ClientId != client.Id)
            {
                return false;
            }

            if (consultation.IsCompleted)
            {
                return false;
            }

            if (consultation.TattooRequest.Status != RequestStatus.WaitingForConsultation)
            {
                return false;
            }

            if (consultation.StartTime <= DateTime.UtcNow.AddHours(24))
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


            var tattooArtistId = consultation.TattooRequest.TattooArtistId;

            var hasConsultationConflict = await context.Consultations.AnyAsync(c =>
                c.Id != id &&
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                dto.EndTime > c.StartTime);

            if (hasConsultationConflict)
            {
                return false;
            }

            var hasTattooSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                dto.EndTime > s.StartTime);

            if (hasTattooSessionConflict)
            {
                return false;
            }

            consultation.StartTime = dto.StartTime;
            consultation.EndTime = dto.EndTime;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteConsultationAsync(
            int id,
            string userId)
        {
            var tattooArtist = await context.TattooArtists
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (tattooArtist == null)
            {
                return false;
            }

            var consultation = await context.Consultations
                .Include(c => c.TattooRequest)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation == null)
            {
                return false;
            }

            if (consultation.TattooRequest.TattooArtistId != tattooArtist.Id)
            {
                return false;
            }

            context.Consultations.Remove(consultation);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteConsultationAsync(
            int tattooRequestId,
            CompleteConsultationDto dto,
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

            if (tattooRequest.Status != RequestStatus.WaitingForConsultation)
            {
                return false;
            }

            var consultation = await context.Consultations
                .FirstOrDefaultAsync(c => c.TattooRequestId == tattooRequestId);

            if (consultation == null)
            {
                return false;
            }

            if (consultation.IsCompleted)
            {
                return false;
            }

            if (dto.SessionsToBook <= 0)
            {
                return false;
            }

            if (dto.PriceForSession == null ||
                dto.PriceForSession.Count != dto.SessionsToBook)
            {
                return false;
            }

            if (dto.PriceForSession.Any(price => price <= 0))
            {
                return false;
            }

            consultation.IsCompleted = true;

            tattooRequest.Status = RequestStatus.ConsultationCompleted;
            tattooRequest.RemainingSessionsToBook = dto.SessionsToBook;
            tattooRequest.PriceForSession = dto.PriceForSession;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RejectConsultationAsync(
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

            if (tattooRequest.Status != RequestStatus.WaitingForConsultation &&
                tattooRequest.Status != RequestStatus.ConsultationCompleted)
            {
                return false;
            }

            var consultation = await context.Consultations
                .FirstOrDefaultAsync(c => c.TattooRequestId == tattooRequestId);

            if (consultation == null)
            {
                return false;
            }

            tattooRequest.Status = RequestStatus.Rejected;

            await context.SaveChangesAsync();

            return true;
        }

        private static GetConsultationDto MapToDto(Consultation consultation)
        {
            return new GetConsultationDto
            {
                StartTime = consultation.StartTime,
                EndTime = consultation.EndTime,
                IsOnline = consultation.IsOnline,
                IsCompleted = consultation.IsCompleted
            };
        }
    }
}