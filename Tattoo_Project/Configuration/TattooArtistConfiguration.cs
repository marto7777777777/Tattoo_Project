using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class TattooArtistConfiguration : IEntityTypeConfiguration<TattooArtist>
    {
        public void Configure(EntityTypeBuilder<TattooArtist> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.StudioName)
                .HasMaxLength(100);

            builder.Property(x => x.StudioAddress)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.HasMany(x => x.Schedules)
                .WithOne(x => x.TattooArtist)
                .HasForeignKey(x => x.TattooArtistId);

            builder.HasMany(x => x.TattooRequests)
                .WithOne(x => x.TattooArtist)
                .HasForeignKey(x => x.TattooArtistId);

            builder.HasMany(x => x.Requirements)
                 .WithOne(x => x.TattooArtist)
                 .HasForeignKey(x => x.TattooArtistId);

            builder
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<TattooArtist>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(a => a.UserId)
                .IsUnique();
        }
    }
}
