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

        public DbSet<Studio> Studios { get; set; }

        public DbSet<StudioJoinRequest> StudioJoinRequests { get; set; }

        public DbSet<TattooRequest> TattooRequests { get; set; }

        public DbSet<TattooReferenceImage> TattooReferenceImages { get; set; }

        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<Consultation> Consultations { get; set; }

        public DbSet<TattooSession> TattooSessions { get; set; }

        public DbSet<ArtistResponse> ArtistResponses { get; set; }

        public DbSet<ArtistReview> ArtistReviews { get; set; }
        public DbSet<ArtistSpecialtyStyle> ArtistSpecialtyStyles { get; set; }
        public DbSet<ClientFavoriteStudio> ClientFavoriteStudios { get; set; }

        public DbSet<ArtistUnavailableDate> ArtistUnavailableDates { get; set; }

        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }

        public DbSet<AiTattooProject> AiTattooProjects { get; set; }
        public DbSet<AiTattooVersion> AiTattooVersions { get; set; }
        public DbSet<AiProjectPayment> AiProjectPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity's default EmailIndex is not unique. InkRoute uses email as an
            // account identifier, so enforce the rule in SQL as well as UserManager.
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");

            // Phone ownership belongs to the User, not to an individual Client/Artist
            // profile. This lets the same user share one number across both profiles
            // while preventing a second account from claiming it.
            modelBuilder.Entity<ApplicationUser>()
                .Property(user => user.PhoneNumber)
                .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(user => user.PhoneNumber)
                .HasDatabaseName("PhoneNumberIndex")
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");

            modelBuilder.ApplyConfigurationsFromAssembly(
                Assembly.GetExecutingAssembly());
        }
    }
}
