using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "dbo");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");
        builder.Property(c => c.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CurrentBalance).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(c => c.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Name);
    }
}
