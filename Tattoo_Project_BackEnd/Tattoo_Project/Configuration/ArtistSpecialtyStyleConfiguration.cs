using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class ArtistSpecialtyStyleConfiguration : IEntityTypeConfiguration<ArtistSpecialtyStyle>
    {
        public void Configure(EntityTypeBuilder<ArtistSpecialtyStyle> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(80);
            builder.HasIndex(x => new { x.TattooArtistId, x.Name }).IsUnique();
            builder.HasOne(x => x.TattooArtist)
                .WithMany(x => x.SpecialtyStyles)
                .HasForeignKey(x => x.TattooArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
