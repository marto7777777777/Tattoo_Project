using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore.Metadata.Builders;using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class ProviderWebhookEventConfiguration:IEntityTypeConfiguration<ProviderWebhookEvent>
{public void Configure(EntityTypeBuilder<ProviderWebhookEvent>b){b.HasKey(x=>x.Id);b.Property(x=>x.Provider).HasMaxLength(30).IsRequired();b.Property(x=>x.ExternalEventId).HasMaxLength(500).IsRequired();b.Property(x=>x.EventType).HasMaxLength(150).IsRequired();b.Property(x=>x.PayloadHash).HasMaxLength(64).IsRequired();b.Property(x=>x.ProcessingStatus).HasMaxLength(30).IsRequired();b.Property(x=>x.Error).HasMaxLength(1000);b.HasIndex(x=>new{x.Provider,x.ExternalEventId}).IsUnique();}}
