using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class ProviderSubscriptionConfiguration:IEntityTypeConfiguration<ProviderSubscription>
{
 public void Configure(EntityTypeBuilder<ProviderSubscription> b)
 {
  b.HasKey(x=>x.Id);b.Property(x=>x.Provider).HasMaxLength(30).IsRequired();b.Property(x=>x.ExternalSubscriptionId).HasMaxLength(500).IsRequired();b.Property(x=>x.ExternalTransactionId).HasMaxLength(500);b.Property(x=>x.ProductId).HasMaxLength(200).IsRequired();b.Property(x=>x.Status).HasMaxLength(40).IsRequired();b.Property(x=>x.PurchaseTokenHash).HasMaxLength(64);b.Property(x=>x.EncryptedPurchasePayload).HasColumnType("nvarchar(max)");b.Property(x=>x.LastProviderEventId).HasMaxLength(500);
  b.HasIndex(x=>new{x.Provider,x.ExternalSubscriptionId}).IsUnique();b.HasIndex(x=>new{x.Provider,x.ExternalTransactionId}).IsUnique().HasFilter("[ExternalTransactionId] IS NOT NULL");b.HasIndex(x=>x.PurchaseTokenHash).IsUnique().HasFilter("[PurchaseTokenHash] IS NOT NULL");
  b.HasOne(x=>x.ArtistSubscription).WithMany(x=>x.ProviderSubscriptions).HasForeignKey(x=>x.ArtistSubscriptionId).OnDelete(DeleteBehavior.Cascade);
 }
}
