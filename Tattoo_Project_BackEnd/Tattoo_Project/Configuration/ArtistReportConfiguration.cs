using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public sealed class ArtistReportConfiguration : IEntityTypeConfiguration<ArtistReport>
{
    public void Configure(EntityTypeBuilder<ArtistReport> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ReporterUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.DecisionNote).HasMaxLength(2000);
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.HasIndex(x => new { x.ReportedArtistId, x.Status });
        b.HasIndex(x => new { x.ReporterUserId, x.ReportedArtistId, x.Status });
        b.HasOne(x => x.ReportedArtist).WithMany().HasForeignKey(x => x.ReportedArtistId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReporterUser).WithMany().HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReviewedByAdmin).WithMany().HasForeignKey(x => x.ReviewedByAdminId).OnDelete(DeleteBehavior.Restrict);
    }
}
