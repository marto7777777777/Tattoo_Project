using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public sealed class ArtistModerationSubmissionConfiguration : IEntityTypeConfiguration<ArtistModerationSubmission>, IEntityTypeConfiguration<ArtistModerationSubmissionImage>
{
    public void Configure(EntityTypeBuilder<ArtistModerationSubmission> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.AdminNote).HasMaxLength(2000);
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.HasIndex(x => new { x.TattooArtistId, x.Status });
        b.HasOne(x => x.TattooArtist).WithMany().HasForeignKey(x => x.TattooArtistId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.SourceReport).WithMany().HasForeignKey(x => x.SourceReportId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ReviewedByAdmin).WithMany().HasForeignKey(x => x.ReviewedByAdminId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ArtistModerationSubmissionImage> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        b.HasOne(x => x.Submission).WithMany(x => x.Images).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
