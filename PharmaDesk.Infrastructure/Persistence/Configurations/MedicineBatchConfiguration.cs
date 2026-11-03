using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Purchasing;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class MedicineBatchConfiguration : IEntityTypeConfiguration<MedicineBatch>
{
    public void Configure(EntityTypeBuilder<MedicineBatch> builder)
    {
        builder.ToTable("MedicineBatches", "dbo");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber).HasMaxLength(100).IsRequired();
        builder.Property(b => b.QuantityReceived).HasColumnType("decimal(18,2)");
        builder.Property(b => b.QuantityRemaining).HasColumnType("decimal(18,2)");
        builder.Property(b => b.PurchaseCostPerBaseUnit).HasColumnType("decimal(18,2)");
        builder.Property(b => b.SellingPricePerBaseUnit).HasColumnType("decimal(18,2)");
        builder.Property(b => b.CreatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(b => b.TenantId);
        builder.HasIndex(b => b.MedicineId);
        builder.HasIndex(b => b.ExpiryDate);

        builder.HasOne<Medicine>().WithMany().HasForeignKey(b => b.MedicineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(b => b.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Purchase>().WithMany().HasForeignKey(b => b.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseItem>().WithMany().HasForeignKey(b => b.PurchaseItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
