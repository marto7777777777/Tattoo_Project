namespace Tattoo_Project.DTOs.ConsultationDTOs
{
    public class CompleteConsultationDto
    {
        public int SessionsToBook { get; set; }
        public List<decimal> PriceForSession { get; set; }
        public List<int>? DurationHoursForSession { get; set; }
    }
}
