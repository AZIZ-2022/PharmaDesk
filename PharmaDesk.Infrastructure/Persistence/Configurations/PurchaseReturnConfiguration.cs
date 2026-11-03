using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Domain.Returns;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("PurchaseReturns", "dbo");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.QuantityReturned).HasColumnType("decimal(18,2)");
        builder.Property(r => r.CostPerBaseUnitAtPurchase).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ReturnAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Reason).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.ReturnedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.MedicineBatchId);

        builder.HasOne<Purchase>().WithMany().HasForeignKey(r => r.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MedicineBatch>().WithMany().HasForeignKey(r => r.MedicineBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(r => r.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
