using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Purchasing;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", "dbo");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.QuantityDelta).HasColumnType("decimal(18,2)");
        builder.Property(a => a.Reason).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.AdjustedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.MedicineBatchId);

        builder.HasOne<MedicineBatch>().WithMany().HasForeignKey(a => a.MedicineBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
