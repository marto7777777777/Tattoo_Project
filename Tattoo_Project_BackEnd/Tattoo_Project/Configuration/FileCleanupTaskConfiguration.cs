using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public class FileCleanupTaskConfiguration : IEntityTypeConfiguration<FileCleanupTask>
{
    public void Configure(EntityTypeBuilder<FileCleanupTask> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(500);
        b.HasIndex(x => new { x.CompletedAt, x.UpdatedAt });
    }
}
