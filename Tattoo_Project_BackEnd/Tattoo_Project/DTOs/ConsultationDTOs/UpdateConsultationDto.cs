namespace Tattoo_Project.DTOs.ConsultationDTOs
{
    public class UpdateConsultationDto
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? Notes { get; set; }
    }
}
