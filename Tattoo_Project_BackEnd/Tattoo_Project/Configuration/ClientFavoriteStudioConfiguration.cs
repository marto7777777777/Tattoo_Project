using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class ClientFavoriteStudioConfiguration : IEntityTypeConfiguration<ClientFavoriteStudio>
    {
        public void Configure(EntityTypeBuilder<ClientFavoriteStudio> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreatedOn).IsRequired();
            builder.HasIndex(x => new { x.ClientId, x.StudioId }).IsUnique();

            builder.HasOne(x => x.Client)
                .WithMany(x => x.FavoriteStudios)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Studio)
                .WithMany(x => x.FavoritedByClients)
                .HasForeignKey(x => x.StudioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
