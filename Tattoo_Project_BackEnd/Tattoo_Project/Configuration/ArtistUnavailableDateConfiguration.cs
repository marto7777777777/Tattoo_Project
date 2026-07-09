using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configurations
{
    public class ArtistUnavailableDateConfiguration : IEntityTypeConfiguration<ArtistUnavailableDate>
    {
        public void Configure(EntityTypeBuilder<ArtistUnavailableDate> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.StartDateTime)
                .IsRequired();

            builder.Property(u => u.EndDateTime)
                .IsRequired();


            builder.HasOne(u => u.TattooArtist)
                .WithMany(a => a.UnavailableDates)
                .HasForeignKey(u => u.TattooArtistId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}