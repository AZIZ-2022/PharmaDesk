using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Sales;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales", "dbo");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PaymentMethod).HasMaxLength(50);
        builder.Property(s => s.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(s => s.SaleDate).HasColumnType("datetime2(0)");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.SaleDate);

        builder.HasOne<Customer>().WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
