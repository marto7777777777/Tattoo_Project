namespace Tattoo_Project.DTOs.ClientDTOs
{
    public class UpdateClientDto
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Country { get; set; } = null!;
    }
}
