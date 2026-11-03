using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Payments;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments", "dbo");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PaymentMethod).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.PaymentDate).HasColumnType("datetime2(0)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.SupplierId);

        builder.HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
