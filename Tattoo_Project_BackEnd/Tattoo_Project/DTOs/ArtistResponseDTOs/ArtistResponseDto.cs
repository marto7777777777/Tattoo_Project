using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs.ArtistResponceDTOs
{
    public class ArtistResponseDto
    {
        public decimal EstimatedPrice { get; set; }

        public int EstimatedHours { get; set; }

        public string ResponseMessage { get; set; } = null!;

        public ArtistResponseWorkflowPath WorkflowPath { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
