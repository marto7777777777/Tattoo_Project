using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
    {
        public void Configure(EntityTypeBuilder<PendingRegistration> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            builder.Property(p => p.UserName).HasMaxLength(256).IsRequired();
            builder.Property(p => p.NormalizedUserName).HasMaxLength(256).IsRequired();
            builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
            builder.Property(p => p.NormalizedEmail).HasMaxLength(256).IsRequired();
            builder.Property(p => p.PasswordHash).IsRequired();
            builder.Property(p => p.VerificationCodeHash).HasMaxLength(64).IsRequired();
            builder.Property(p => p.TermsVersion).HasMaxLength(50).IsRequired();
            builder.Property(p => p.PrivacyVersion).HasMaxLength(50).IsRequired();

            builder.HasIndex(p => p.NormalizedEmail).IsUnique();
            builder.HasIndex(p => p.NormalizedUserName).IsUnique();
            builder.HasIndex(p => p.ExpiresAt);
        }
    }
}
