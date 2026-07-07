using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configurations
{
    public class ArtistReviewConfiguration : IEntityTypeConfiguration<ArtistReview>
    {
        public void Configure(EntityTypeBuilder<ArtistReview> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Rating)
                .IsRequired();

            builder.Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.Property(r => r.CreatedOn)
                .IsRequired();

            builder.HasOne(r => r.Client)
                .WithMany(c => c.ArtistReviews)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.TattooArtist)
                .WithMany(a => a.Reviews)
                .HasForeignKey(r => r.TattooArtistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.TattooRequest)
                .WithOne(r => r.ArtistReview)
                .HasForeignKey<ArtistReview>(r => r.TattooRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.TattooRequestId)
                .IsUnique();
        }
    }
}