using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class AiProjectPaymentConfiguration : IEntityTypeConfiguration<AiProjectPayment>
{
 public void Configure(EntityTypeBuilder<AiProjectPayment> b){
  b.HasKey(x=>x.Id); b.Property(x=>x.StripeCheckoutSessionId).HasMaxLength(255).IsRequired(); b.HasIndex(x=>x.StripeCheckoutSessionId).IsUnique();
  b.Property(x=>x.Currency).HasMaxLength(3); b.Property(x=>x.Status).HasMaxLength(30);
  b.HasOne(x=>x.AiTattooProject).WithMany(x=>x.Payments).HasForeignKey(x=>x.AiTattooProjectId).OnDelete(DeleteBehavior.Cascade);
 }
}
