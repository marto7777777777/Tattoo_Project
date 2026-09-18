using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public class ArtistSubscriptionConfiguration : IEntityTypeConfiguration<ArtistSubscription>
{
    public void Configure(EntityTypeBuilder<ArtistSubscription> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.ActiveProvider).HasMaxLength(30);
        b.Property(x => x.PendingProvider).HasMaxLength(30);
        b.Property(x => x.GoogleObfuscatedAccountId).HasMaxLength(64);
        b.Property(x => x.StripeCustomerId).HasMaxLength(255);
        b.Property(x => x.StripeSubscriptionId).HasMaxLength(255);
        b.Property(x => x.StripeCheckoutSessionId).HasMaxLength(255);
        b.HasIndex(x => x.TattooArtistId).IsUnique();
        b.HasIndex(x => x.StripeCustomerId).IsUnique().HasFilter("[StripeCustomerId] IS NOT NULL");
        b.HasIndex(x => x.StripeSubscriptionId).IsUnique().HasFilter("[StripeSubscriptionId] IS NOT NULL");
        b.HasIndex(x => x.StripeCheckoutSessionId).IsUnique().HasFilter("[StripeCheckoutSessionId] IS NOT NULL");
        b.HasIndex(x => x.GoogleObfuscatedAccountId).IsUnique().HasFilter("[GoogleObfuscatedAccountId] IS NOT NULL");
        b.HasIndex(x => x.AppleAppAccountToken).IsUnique().HasFilter("[AppleAppAccountToken] IS NOT NULL");
        b.HasOne(x => x.TattooArtist).WithOne(x => x.Subscription).HasForeignKey<ArtistSubscription>(x => x.TattooArtistId).OnDelete(DeleteBehavior.Cascade);
    }
}
