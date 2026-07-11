using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class TattooRequestConfiguration : IEntityTypeConfiguration<TattooRequest>
    {
        public void Configure(EntityTypeBuilder<TattooRequest> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.Placement)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.TattooStyle)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasMany(x => x.Images)
                .WithOne(x => x.TattooRequest)
                .HasForeignKey(x => x.TattooRequestId);

            builder.HasOne(x => x.Consultation)
                .WithOne(x => x.TattooRequest)
                .HasForeignKey<Consultation>(x => x.TattooRequestId);

            builder.HasOne(x => x.ArtistResponse)
                .WithOne(x => x.TattooRequest)
                .HasForeignKey<ArtistResponse>(x => x.TattooRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
