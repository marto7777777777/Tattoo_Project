using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class ArtistRequirementConfiguration : IEntityTypeConfiguration<ArtistRequirement>
    {
        public void Configure(EntityTypeBuilder<ArtistRequirement> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(x => x.TattooArtist)
                .WithMany(x => x.Requirements)
                .HasForeignKey(x => x.TattooArtistId);
        }
    }
}
