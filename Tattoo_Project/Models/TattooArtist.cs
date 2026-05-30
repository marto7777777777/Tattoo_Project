namespace Tattoo_Project.Models
{
    public class TattooArtist
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string StudioName { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string StudioAddress { get; set; } = null!;

        public ICollection<TattooRequest> TattooRequests { get; set; }
            = new List<TattooRequest>();

        public ICollection<Schedule> Schedules { get; set; } 
            = new List<Schedule>();
    }
}
