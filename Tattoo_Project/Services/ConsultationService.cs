using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class ConsultationService(TattooDbContext context) : IConsultationService
    {
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

            var alreadyHasConsultation = await context.Consultations
                .AnyAsync(c => c.TattooRequestId == dto.TattooRequestId);

            if (alreadyHasConsultation)
            {
                return false;
            }

            if (dto.StartTime <= DateTime.UtcNow)
            {
                return false;
            }

            if (dto.EndTime <= DateTime.UtcNow)
            {
                return false;
            }

            if (dto.StartTime >= dto.EndTime)
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

            var hasSessionConflict = await context.TattooSessions.AnyAsync(s =>
                s.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < s.EndTime &&
                dto.EndTime > s.StartTime);

            if (hasSessionConflict)
            {
                return false;
            }

            if (dto.EndTime - dto.StartTime < TimeSpan.FromMinutes(15))
            {
                return false;
            }

            Consultation consultation = new()
            {
                TattooRequestId = dto.TattooRequestId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Notes = dto.Notes,
                IsCompleted = false
            };

            context.Consultations.Add(consultation);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteConsultationAsync(int id)
        {
            var consultation = await context.Consultations.FirstOrDefaultAsync(x => x.Id == id);

            if (consultation == null)
            {
                return false; // Няма такава консултация
            }
            if (consultation.StartTime <= DateTime.UtcNow.AddHours(24))
            {
                return false; // Остават по малко от 24 часа
            }

            context.Consultations.Remove(consultation);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GetConsultationDto>> GetAllConsultationsAsync()
        {
            return !context.Consultations.Any() ? null
                : await context.Consultations.Select(c => new GetConsultationDto
            {
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                IsOnline = c.IsOnline,
                Notes = c.Notes
            }).ToListAsync();
        }

        public async Task<GetConsultationDto> GetConsultationByIdAsync(int id)
        {
            var consultation = await context.Consultations.FirstOrDefaultAsync(x => x.Id == id);
            if (consultation == null)
            {
                return null;
            }
            var cosultationDto = new GetConsultationDto
            {
                StartTime = consultation.StartTime,
                EndTime = consultation.EndTime,
                IsOnline = consultation.IsOnline,
                Notes = consultation.Notes
            };
            return cosultationDto;
        }

        public async Task<bool> UpdateConsultationAsync(int id, UpdateConsultationDto dto)
        {
            var consultation = await context.Consultations.FirstOrDefaultAsync(x => x.Id == id);

            if (consultation == null)
            {
                return false; // Няма такава консултация
            }

            if (consultation.StartTime <= DateTime.UtcNow.AddHours(24))
            {
                return false; // Остават по малко от 24 часа
            }

            if (dto.StartTime >= dto.EndTime)
            {
                return false; //Валиден интервал ли е
            }

            if(dto.StartTime < DateTime.UtcNow)
            {
                return false; // Не е ли в минало време
            }

            var tattooArtistId = await context.TattooRequests
                .Where(tr => tr.Id == consultation.TattooRequestId)
                .Select(tr => tr.TattooArtistId)
                .SingleAsync();

            var hasConflict = await context.Consultations.AnyAsync(c =>
                c.Id != consultation.Id &&
                c.TattooRequest.TattooArtistId == tattooArtistId &&
                dto.StartTime < c.EndTime &&
                dto.EndTime > c.StartTime);

            if (hasConflict)
            {
                return false; // Свободен ли е татуиста
            }

            if (dto.EndTime - dto.StartTime < TimeSpan.FromMinutes(15))
            {
                return false; //минимална продължителност
            }

            consultation.Notes = dto.Notes;
            consultation.StartTime = dto.StartTime;
            consultation.EndTime = dto.EndTime;

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
    }
}
