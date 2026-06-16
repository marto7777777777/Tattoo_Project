using Tattoo_Project.DTOs.ConsultationDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IConsultationService
    {
        Task<ICollection<GetConsultationDto>> GetAllConsultationsAsync();

        Task<GetConsultationDto?> GetConsultationByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist);

        Task<bool> CreateConsultationAsync(
            CreateConsultationDto dto,
            string userId);

        Task<bool> UpdateConsultationAsync(
            int id,
            UpdateConsultationDto dto,
            string userId);

        Task<bool> DeleteConsultationAsync(
            int id,
            string userId);

        Task<bool> CompleteConsultationAsync(
            int tattooRequestId,
            CompleteConsultationDto dto,
            string userId);

        Task<bool> RejectConsultationAsync(
            int tattooRequestId,
            string userId);
    }
}