namespace Tattoo_Project.DTOs
{
    public class ConsultationDto
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsOnline { get; set; }

        public string? Notes { get; set; }
    }
}
