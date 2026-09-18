using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public class PendingEmailChangeConfiguration : IEntityTypeConfiguration<PendingEmailChange>
{
    public void Configure(EntityTypeBuilder<PendingEmailChange> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NewEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NormalizedNewEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.UsedAt });
        builder.HasIndex(x => x.NormalizedNewEmail);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
