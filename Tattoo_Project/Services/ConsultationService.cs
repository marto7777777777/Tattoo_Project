using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class ConsultationService(TattooDbContext context) : IConsultationService
    {
        public async Task<bool> CreateConsultationAsync(CreateConsultationDto dto)
        {
            var tattooRequest = await context.TattooRequests
        .FirstOrDefaultAsync(tr => tr.Id == dto.TattooRequestId);

            // 1. Съществува ли такъв tattoorequest
            if (tattooRequest == null)
            {
                return false;
            }

            // 2. Не може да се създаде consultation преди tattooResponse.
            if (tattooRequest.Status != RequestStatus.WaitingForConsultation)
            {
                return false;
            }

            // 3. Валиден интервал ли е
            if (dto.StartTime >= dto.EndTime)
            {
                return false;
            }

            // 4. Не е ли в миналото (по желание)
            if (dto.StartTime < DateTime.UtcNow)
            {
                return false;
            }

            // 5. Има ли вече консултация за тази заявка
            var consultationExists = await context.Consultations
                .AnyAsync(c => c.TattooRequestId == dto.TattooRequestId);

            if (consultationExists)
            {
                return false;
            }

            // 6. Свободен ли е татуистъта проверка за консултации
            var hasConflict = await context.Consultations
                .AnyAsync(c =>
                    c.TattooRequest.TattooArtistId == tattooRequest.TattooArtistId &&
                    dto.StartTime < c.EndTime &&
                    dto.EndTime > c.StartTime);

            if (hasConflict)
            {
                return false; 
            }

            //7.Свободен ли е татуйста провека за сесии
            var hasSessionConflict = await context.TattooSessions
                .AnyAsync(s =>
            s.TattooRequest.TattooArtistId == tattooRequest.TattooArtistId &&
            dto.StartTime < s.EndTime &&
            dto.EndTime > s.StartTime);

            if (hasSessionConflict)
            {
                return false;
            }

            await context.Consultations.AddAsync(new Models.Consultation
            {
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Notes = dto.Notes,
                TattooRequestId = dto.TattooRequestId
            });

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

        public async Task<bool> CompleteConsultationAsync(int tattooRequestId, CompleteConsultationDto dto)
        {
            var request = await context.TattooRequests.FirstOrDefaultAsync(x => x.Id == tattooRequestId);

            if (request is null)
            {
                return false;
            }

            if (request.Status != RequestStatus.WaitingForConsultation)
            {
                return false;
            }

            var consultationExists = await context.Consultations
                .AnyAsync(x => x.TattooRequestId == tattooRequestId);

            if (!consultationExists)
            {
                return false;
            }

            request.Status = RequestStatus.ConsultationCompleted;


            //Броя на сесиите равен ли е на броя на изброените цени
            if (dto.SessionsToBook != dto.PriceForSession.Count)
            {
                return false;
            }

            request.RemainingSessionsToBook = dto.SessionsToBook;
            request.PriceForSession = dto.PriceForSession;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RejectConsultationAsync(int tattooRequestId)
        {
            var request = await context.TattooRequests.FirstOrDefaultAsync(x => x.Id == tattooRequestId);

            if (request is null)
            {
                return false;
            }

            if (request.Status != RequestStatus.WaitingForConsultation)
            {
                return false;
            }

            var consultationExists = await context.Consultations
                .AnyAsync(x => x.TattooRequestId == tattooRequestId);

            if (!consultationExists)
            {
                return false;
            }

            request.Status = RequestStatus.Rejected;

            await context.SaveChangesAsync();

            return true;
        }
    }
}
