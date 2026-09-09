using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class AnalyticsOutboxEventConfiguration : IEntityTypeConfiguration<AnalyticsOutboxEvent>
{ public void Configure(EntityTypeBuilder<AnalyticsOutboxEvent> b){b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(100).IsRequired();b.HasIndex(x=>new{x.TattooArtistId,x.DispatchedAt});} }
