using Tattoo_Project.Models;

namespace Tattoo_Project.DTOs
{
    public class GetClientDto
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public List<ClientTattooRequestsDto> ClientTattooRequestsDto {  get; set; } = new List<ClientTattooRequestsDto>();
    }
}
