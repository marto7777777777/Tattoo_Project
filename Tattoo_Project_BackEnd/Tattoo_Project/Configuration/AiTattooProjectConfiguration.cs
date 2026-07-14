using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class AiTattooProjectConfiguration : IEntityTypeConfiguration<AiTattooProject>
{
 public void Configure(EntityTypeBuilder<AiTattooProject> b){
  b.HasKey(x=>x.Id); b.Property(x=>x.Title).HasMaxLength(140).IsRequired();
  b.Property(x=>x.TattooStyle).HasMaxLength(100).IsRequired(); b.Property(x=>x.Placement).HasMaxLength(100).IsRequired();
  b.Property(x=>x.InitialDescription).HasMaxLength(3000).IsRequired();
  b.HasIndex(x=>new{x.UserId,x.IsFreeProject}).HasFilter("[IsFreeProject] = 1").IsUnique();
  b.HasOne(x=>x.User).WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);
 }
}
