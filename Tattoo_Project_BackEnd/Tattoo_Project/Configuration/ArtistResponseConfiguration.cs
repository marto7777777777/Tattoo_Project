using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class ArtistResponseConfiguration : IEntityTypeConfiguration<ArtistResponse>
    {
        public void Configure(EntityTypeBuilder<ArtistResponse> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EstimatedPrice)
                .IsRequired();

            builder.Property(x => x.ResponseMessage)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedOn)
                .IsRequired();

            builder.HasOne(x => x.TattooRequest)
                .WithOne(x => x.ArtistResponse)
                .HasForeignKey<ArtistResponse>(x => x.TattooRequestId);
        }
    }
}
