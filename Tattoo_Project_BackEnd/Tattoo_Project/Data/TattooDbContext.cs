using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Tattoo_Project.Models;

namespace Tattoo_Project.Data
{
    public class TattooDbContext : IdentityDbContext<ApplicationUser>
    {
        public TattooDbContext(DbContextOptions<TattooDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }

        public DbSet<TattooArtist> TattooArtists { get; set; }

        public DbSet<TattooRequest> TattooRequests { get; set; }

        public DbSet<TattooReferenceImage> TattooReferenceImages { get; set; }

        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<Consultation> Consultations { get; set; }

        public DbSet<TattooSession> TattooSessions { get; set; }

        public DbSet<ArtistResponse> ArtistResponses { get; set; }

        public DbSet<ArtistReview> ArtistReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                Assembly.GetExecutingAssembly());
        }
    }
}