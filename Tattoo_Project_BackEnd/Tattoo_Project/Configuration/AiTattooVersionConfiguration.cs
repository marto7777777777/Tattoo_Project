using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class AiTattooVersionConfiguration : IEntityTypeConfiguration<AiTattooVersion>
{
 public void Configure(EntityTypeBuilder<AiTattooVersion> b){
  b.HasKey(x=>x.Id); b.Property(x=>x.Prompt).HasMaxLength(3000).IsRequired(); b.Property(x=>x.ImageUrl).HasMaxLength(500).IsRequired();
  b.HasIndex(x=>new{x.AiTattooProjectId,x.VersionNumber}).IsUnique();
  b.HasOne(x=>x.AiTattooProject).WithMany(x=>x.Versions).HasForeignKey(x=>x.AiTattooProjectId).OnDelete(DeleteBehavior.Cascade);
  b.HasOne(x=>x.ParentVersion).WithMany().HasForeignKey(x=>x.ParentVersionId).OnDelete(DeleteBehavior.NoAction);
 }
}
