namespace Tattoo_Project.DTOs.TattooSessionDTOs
{
    public class AddAdditionalSessionsDto
    {
        public int AdditionalSessions { get; set; }
        public List<decimal> PriceForSession { get; set; }
        public List<int>? DurationHoursForSession { get; set; }
    }
}
