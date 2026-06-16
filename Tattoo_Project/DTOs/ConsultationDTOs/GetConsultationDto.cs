namespace Tattoo_Project.DTOs.ConsultationDTOs
{
    public class GetConsultationDto
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsOnline { get; set; }

        public string? Notes { get; set; }
        public bool? IsCompleted { get; set; }
    }
}
