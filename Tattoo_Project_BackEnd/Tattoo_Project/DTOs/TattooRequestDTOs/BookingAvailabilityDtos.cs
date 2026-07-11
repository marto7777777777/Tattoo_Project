using Microsoft.AspNetCore.Http;

namespace Tattoo_Project.DTOs.TattooRequestDTOs
{
    public class CreateTattooRequestWithImagesDto
    {
        public string Description { get; set; } = null!;
        public string Placement { get; set; } = null!;
        public string TattooStyle { get; set; } = null!;
        public int TattooArtistId { get; set; }
        public ICollection<IFormFile> Images { get; set; } = new List<IFormFile>();
    }

    public class BookingAvailabilityDayDto
    {
        public string Date { get; set; } = null!;
        public bool IsAvailableDay { get; set; }
        public string? Reason { get; set; }
        public ICollection<BookingAvailabilitySlotDto> Slots { get; set; } = new List<BookingAvailabilitySlotDto>();
    }

    public class BookingAvailabilitySlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Label { get; set; } = null!;
    }

    public class BookingAvailabilityDto
    {
        public int TattooRequestId { get; set; }
        public string BookingType { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public ICollection<BookingAvailabilityDayDto> Days { get; set; } = new List<BookingAvailabilityDayDto>();
    }
}
