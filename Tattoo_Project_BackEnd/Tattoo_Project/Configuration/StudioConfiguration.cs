using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class StudioConfiguration : IEntityTypeConfiguration<Studio>
    {
        public void Configure(EntityTypeBuilder<Studio> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(1500);
            builder.Property(x => x.Address).IsRequired().HasMaxLength(220);
            builder.Property(x => x.City).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Country).IsRequired().HasMaxLength(100);
            builder.Property(x => x.IsOpenForJoinRequests).HasDefaultValue(true);

            builder.HasIndex(x => new { x.Name, x.City });

            builder.HasOne(x => x.OwnerArtist)
                .WithOne()
                .HasForeignKey<Studio>(x => x.OwnerArtistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Artists)
                .WithOne(x => x.Studio)
                .HasForeignKey(x => x.StudioId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
