using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore.Metadata.Builders;using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class AiProjectStorePurchaseConfiguration:IEntityTypeConfiguration<AiProjectStorePurchase>
{
 public void Configure(EntityTypeBuilder<AiProjectStorePurchase>b)
 {
  b.HasKey(x=>x.Id);b.Property(x=>x.Provider).HasMaxLength(30).IsRequired();b.Property(x=>x.ExternalTransactionId).HasMaxLength(500).IsRequired();b.Property(x=>x.ProductId).HasMaxLength(200).IsRequired();b.Property(x=>x.PurchasePayloadHash).HasMaxLength(64).IsRequired();b.Property(x=>x.EncryptedPurchasePayload).HasColumnType("nvarchar(max)").IsRequired();b.Property(x=>x.ProcessingState).HasMaxLength(30).IsRequired();b.Property(x=>x.LastErrorCode).HasMaxLength(80);b.HasIndex(x=>new{x.Provider,x.ExternalTransactionId}).IsUnique();b.HasIndex(x=>x.PurchasePayloadHash).IsUnique();b.HasIndex(x=>new{x.ProcessingState,x.NextRetryAtUtc});b.HasOne(x=>x.AiTattooProject).WithMany().HasForeignKey(x=>x.AiTattooProjectId).OnDelete(DeleteBehavior.SetNull);
 }
}
