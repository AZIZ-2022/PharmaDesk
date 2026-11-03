using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Domain.Purchasing;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems", "dbo");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.BatchNumber).HasMaxLength(100).IsRequired();
        builder.Property(i => i.QuantityPacks).HasColumnType("decimal(18,2)");
        builder.Property(i => i.PurchasePricePerPack).HasColumnType("decimal(18,2)");
        builder.Property(i => i.SellingPricePerBaseUnit).HasColumnType("decimal(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasIndex(i => i.PurchaseId);

        builder.HasOne<Purchase>().WithMany().HasForeignKey(i => i.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Medicine>().WithMany().HasForeignKey(i => i.MedicineId).OnDelete(DeleteBehavior.Restrict);
    }
}
