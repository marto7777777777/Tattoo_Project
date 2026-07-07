using Tattoo_Project.Models;
using Tattoo_Project.DTOs.TattooRequestDTOs;

namespace Tattoo_Project.DTOs.ClientDTOs
{
    public class GetClientDto
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Country { get; set; } = null!;

        public List<TattooRequestDto>? ClientTattooRequestsDto {  get; set; } = new List<TattooRequestDto>();
    }
}
