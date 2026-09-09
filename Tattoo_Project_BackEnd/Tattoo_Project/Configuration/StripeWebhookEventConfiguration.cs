using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> b){b.HasKey(x=>x.Id);b.Property(x=>x.StripeEventId).HasMaxLength(255).IsRequired();b.HasIndex(x=>x.StripeEventId).IsUnique();b.Property(x=>x.EventType).HasMaxLength(120).IsRequired();b.Property(x=>x.Status).HasMaxLength(30).IsRequired();b.Property(x=>x.Error).HasMaxLength(1000);}
}
