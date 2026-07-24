using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class StudioJoinRequestConfiguration : IEntityTypeConfiguration<StudioJoinRequest>
    {
        public void Configure(EntityTypeBuilder<StudioJoinRequest> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.StudioId, x.Status });
            builder.HasIndex(x => new { x.TattooArtistId, x.Status });

            builder.HasOne(x => x.Studio)
                .WithMany(x => x.JoinRequests)
                .HasForeignKey(x => x.StudioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.TattooArtist)
                .WithMany(x => x.StudioJoinRequests)
                .HasForeignKey(x => x.TattooArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
