namespace Tattoo_Project.DTOs.ConsultationDTOs
{
    public class ConsultationDto
    {
        public int Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsOnline { get; set; }

        public string? Notes { get; set; }
    }
}
