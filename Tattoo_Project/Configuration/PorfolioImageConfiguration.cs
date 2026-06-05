using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class PorfolioImageConfiguration : IEntityTypeConfiguration<PortfolioImage>
    {
        public void Configure(EntityTypeBuilder<PortfolioImage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(x => x.TattooArtist)
                .WithMany(x => x.PortfolioImages)
                .HasForeignKey(x => x.TattooArtistId);
        }
    }
}
