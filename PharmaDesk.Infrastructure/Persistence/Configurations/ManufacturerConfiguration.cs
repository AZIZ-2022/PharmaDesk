using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("Manufacturers", "dbo");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Country).HasMaxLength(100);
        builder.Property(m => m.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(m => m.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(m => m.TenantId);
    }
}
