using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configurations
{
    public class ClientFavoriteArtistConfiguration : IEntityTypeConfiguration<ClientFavoriteArtist>
    {
        public void Configure(EntityTypeBuilder<ClientFavoriteArtist> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.CreatedOn)
                .IsRequired();

            builder.HasOne(f => f.Client)
                .WithMany(c => c.FavoriteArtists)
                .HasForeignKey(f => f.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(f => new { f.ClientId, f.TattooArtistId })
                .IsUnique();
        }
    }
}