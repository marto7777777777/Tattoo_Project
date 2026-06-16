using Tattoo_Project.DTOs.ConsultationDTOs;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IConsultationService
    {
        Task<ResultService<ICollection<GetConsultationDto>>> GetAllConsultationsAsync();

        Task<ResultService<GetConsultationDto>> GetConsultationByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist);

        Task<ResultService> CreateConsultationAsync(
            CreateConsultationDto dto,
            string userId);

        Task<ResultService> UpdateConsultationAsync(
            int id,
            UpdateConsultationDto dto,
            string userId);

        Task<ResultService> DeleteConsultationAsync(
            int id,
            string userId);

        Task<ResultService> CompleteConsultationAsync(
            int tattooRequestId,
            CompleteConsultationDto dto,
            string userId);

        Task<ResultService> RejectConsultationAsync(
            int tattooRequestId,
            string userId);
    }
}