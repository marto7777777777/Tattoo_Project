using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public class AiProjectCheckoutAttemptConfiguration : IEntityTypeConfiguration<AiProjectCheckoutAttempt>
{
    public void Configure(EntityTypeBuilder<AiProjectCheckoutAttempt> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Product).HasMaxLength(80).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.PriceSemantics).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.StripeIdempotencyKey).HasMaxLength(255).IsRequired();
        b.Property(x => x.StripeCheckoutSessionId).HasMaxLength(255);
        b.Property(x => x.StripeCheckoutUrl).HasMaxLength(2048);
        b.Property(x => x.FailureCode).HasMaxLength(80);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.StripeIdempotencyKey).IsUnique();
        b.HasIndex(x => x.StripeCheckoutSessionId).IsUnique().HasFilter("[StripeCheckoutSessionId] IS NOT NULL");
        b.HasIndex(x => new { x.UserId, x.AiTattooProjectId, x.Product })
            .IsUnique()
            .HasFilter("[Status] IN ('Creating','SessionCreated','PaymentConfirmed','GrantPending')");
        b.HasIndex(x => new { x.Status, x.UpdatedAtUtc });
        b.HasOne(x => x.AiTattooProject).WithMany().HasForeignKey(x => x.AiTattooProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
