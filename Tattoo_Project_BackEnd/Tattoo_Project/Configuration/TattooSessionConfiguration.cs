using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tattoo_Project.Models;

namespace Tattoo_Project.Configuration
{
    public class TattooSessionConfiguration : IEntityTypeConfiguration<TattooSession>
    {
        public void Configure(EntityTypeBuilder<TattooSession> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StartTime)
                .IsRequired();

            builder.Property(x => x.EndTime)
                .IsRequired();

            builder.HasOne(x => x.TattooRequest)
                .WithMany(x => x.TattooSessions)
                .HasForeignKey(x => x.TattooRequestId);
        }
    }
}
