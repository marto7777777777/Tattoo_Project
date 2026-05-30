using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class TattooReferenceImageConfiguration : IEntityTypeConfiguration<TattooReferenceImage>
    {
        public void Configure(EntityTypeBuilder<TattooReferenceImage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);
        }
    }
}
