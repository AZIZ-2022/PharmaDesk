using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Returns;
using PharmaDesk.Domain.Sales;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns", "dbo");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.QuantityReturned).HasColumnType("decimal(18,2)");
        builder.Property(r => r.UnitPriceAtSale).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ReturnAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Reason).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.ReturnedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.SaleItemId).IsUnique();

        builder.HasOne<Sale>().WithMany().HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SaleItem>().WithMany().HasForeignKey(r => r.SaleItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
