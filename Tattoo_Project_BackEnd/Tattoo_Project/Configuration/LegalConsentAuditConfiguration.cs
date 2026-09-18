using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration;

public class LegalConsentAuditConfiguration : IEntityTypeConfiguration<LegalConsentAudit>
{
    public void Configure(EntityTypeBuilder<LegalConsentAudit> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.TermsVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PrivacyVersion).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.AcceptedAt });
    }
}
