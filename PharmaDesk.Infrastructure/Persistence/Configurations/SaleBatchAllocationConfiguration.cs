using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Domain.Sales;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SaleBatchAllocationConfiguration : IEntityTypeConfiguration<SaleBatchAllocation>
{
    public void Configure(EntityTypeBuilder<SaleBatchAllocation> builder)
    {
        builder.ToTable("SaleBatchAllocations", "dbo");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.QuantityAllocated).HasColumnType("decimal(18,2)");
        builder.Property(a => a.CostPerBaseUnitAtSale).HasColumnType("decimal(18,2)");

        builder.HasIndex(a => a.SaleItemId);
        builder.HasIndex(a => a.MedicineBatchId);

        builder.HasOne<SaleItem>().WithMany().HasForeignKey(a => a.SaleItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MedicineBatch>().WithMany().HasForeignKey(a => a.MedicineBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
