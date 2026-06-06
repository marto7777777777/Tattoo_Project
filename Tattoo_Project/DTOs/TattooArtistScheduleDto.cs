using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs
{
    public class TattooArtistScheduleDto
    {

        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }
    }
}
