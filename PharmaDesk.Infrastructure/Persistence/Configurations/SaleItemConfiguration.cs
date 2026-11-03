using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Domain.Sales;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems", "dbo");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.QuantityBaseUnits).HasColumnType("decimal(18,2)");
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasIndex(i => i.SaleId);

        builder.HasOne<Sale>().WithMany().HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Medicine>().WithMany().HasForeignKey(i => i.MedicineId).OnDelete(DeleteBehavior.Restrict);
    }
}
