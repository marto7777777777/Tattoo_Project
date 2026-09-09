using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class ArtistAnalyticsMilestoneConfiguration : IEntityTypeConfiguration<ArtistAnalyticsMilestone>
{
    public void Configure(EntityTypeBuilder<ArtistAnalyticsMilestone> b){b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(100).IsRequired();b.HasIndex(x=>new{x.TattooArtistId,x.Name}).IsUnique();b.HasOne(x=>x.TattooArtist).WithMany(x=>x.AnalyticsMilestones).HasForeignKey(x=>x.TattooArtistId).OnDelete(DeleteBehavior.Cascade);}
}
