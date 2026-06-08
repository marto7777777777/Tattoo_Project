using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.TattooArtistDTOs
{
    public class GetTattooArtistDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;

        public string StudioName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string StudioAddress { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public bool OffersOnlineConsultation { get; set; }

        public bool RequiresDeposit { get; set; }

        public decimal? DepositAmount { get; set; }
        public ICollection<TattooArtistRequirementsDto> Requirements { get; set; }
           = new List<TattooArtistRequirementsDto>();
        public ICollection<TattooArtistPortfolioImageDto> PortfolioImages { get; set; }
            = new List<TattooArtistPortfolioImageDto>();

        public ICollection<TattooRequestDto>? TattooRequests { get; set; }
            = new List<TattooRequestDto>();

        public ICollection<TattooArtistScheduleDto> Schedules { get; set; }
            = new List<TattooArtistScheduleDto>();
    }
}
