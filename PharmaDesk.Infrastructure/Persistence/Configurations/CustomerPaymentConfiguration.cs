using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Payments;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments", "dbo");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PaymentMethod).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.Property(p => p.PaymentDate).HasColumnType("datetime2(0)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.CustomerId);

        builder.HasOne<Customer>().WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
