using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickShop.Api.Domain;

namespace QuickShop.Api.Data.Configurations;

/// <summary>Extra configuration for the Identity user table.</summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();

        // Identity defaults are fine otherwise; email is our login identifier.
        builder.HasIndex(u => u.Email);
    }
}
