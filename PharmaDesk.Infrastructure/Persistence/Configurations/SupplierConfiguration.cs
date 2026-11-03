using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "dbo");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(s => s.CurrentBalance).HasColumnType("decimal(18,2)");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(s => s.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.Name);
    }
}
