using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.TattooArtistDTOs
{
    public class CreateTattooArtistDto
    {
        public string Description { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public int ConsultationDurationMinutes { get; set; }
        public bool OffersOnlineConsultation { get; set; }
        public bool RequiresDeposit { get; set; }
        public decimal? DepositAmount { get; set; }

        public StudioSetupMode StudioSetupMode { get; set; }
        public CreateStudioDto? Studio { get; set; }
        public int? JoinStudioId { get; set; }

        public ICollection<TattooArtistRequirementsDto> Requirements { get; set; } = new List<TattooArtistRequirementsDto>();
        public ICollection<TattooArtistPortfolioImageDto> PortfolioImages { get; set; } = new List<TattooArtistPortfolioImageDto>();
        public ICollection<TattooArtistScheduleDto> Schedules { get; set; } = new List<TattooArtistScheduleDto>();
    }
}
