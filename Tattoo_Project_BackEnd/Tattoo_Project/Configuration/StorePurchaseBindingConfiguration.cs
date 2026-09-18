using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore.Metadata.Builders;using Tattoo_Project.Models;
namespace Tattoo_Project.Configuration;
public class StorePurchaseBindingConfiguration:IEntityTypeConfiguration<StorePurchaseBinding>{public void Configure(EntityTypeBuilder<StorePurchaseBinding>b){b.HasKey(x=>x.Id);b.Property(x=>x.Provider).HasMaxLength(30).IsRequired();b.Property(x=>x.BindingKeyHash).HasMaxLength(64).IsRequired();b.HasIndex(x=>new{x.Provider,x.BindingKeyHash}).IsUnique();}}
