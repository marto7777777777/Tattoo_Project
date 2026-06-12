using Tattoo_Project.DTOs.ConsultationDTOs;

namespace Tattoo_Project.Services.Interfaces
{
    public interface IConsultationService
    {
        Task<List<GetConsultationDto>> GetAllConsultationsAsync();
        Task<GetConsultationDto> GetConsultationByIdAsync(int id);
        Task<bool> CreateConsultationAsync(CreateConsultationDto dto);
        Task<bool> UpdateConsultationAsync(int id,UpdateConsultationDto dto);
        Task<bool> DeleteConsultationAsync(int id);
        Task<bool> CompleteConsultationAsync(int tattooRequestId, CompleteConsultationDto dto);
        Task<bool> RejectConsultationAsync(int tattooRequestId);
    }
}
