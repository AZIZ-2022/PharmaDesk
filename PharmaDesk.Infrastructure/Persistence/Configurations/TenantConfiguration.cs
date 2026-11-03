using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Organization;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "dbo");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.LicenseNumber).HasMaxLength(100);
        builder.Property(t => t.Phone).HasMaxLength(30);
        builder.Property(t => t.Email).HasMaxLength(256);
        builder.Property(t => t.Address).HasMaxLength(500);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(t => t.ModifiedAtUtc).HasColumnType("datetime2(0)");
    }
}
