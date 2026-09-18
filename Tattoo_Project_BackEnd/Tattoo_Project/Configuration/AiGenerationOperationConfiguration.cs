using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public class AiGenerationOperationConfiguration : IEntityTypeConfiguration<AiGenerationOperation>
{
    public void Configure(EntityTypeBuilder<AiGenerationOperation> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.OperationType).HasMaxLength(20).IsRequired();
        b.Property(x => x.RequestKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.Instruction).HasMaxLength(3000);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.FailureCode).HasMaxLength(80);
        b.HasIndex(x => x.OperationId).IsUnique();
        b.HasIndex(x => new { x.AiTattooProjectId, x.Status });
        b.HasIndex(x => new { x.Status, x.LeaseExpiresAtUtc });
        b.HasOne(x => x.AiTattooProject).WithMany().HasForeignKey(x => x.AiTattooProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
