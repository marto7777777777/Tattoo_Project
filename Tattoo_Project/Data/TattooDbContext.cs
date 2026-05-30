using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Models;

namespace Tattoo_Project.Data
{
    public class TattooDbContext : DbContext
    {
        public TattooDbContext(DbContextOptions<TattooDbContext> options) : base(options)
        {
        }

        public DbSet<TattooArtist> TattooArtist { get; set; }
    }
}
