using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Purchasing;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", "dbo");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.InvoiceNumber).HasMaxLength(100);
        builder.Property(p => p.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(p => p.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PurchaseDate).HasColumnType("datetime2(0)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.PurchaseDate);

        builder.HasOne<PharmaDesk.Domain.Partners.Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
