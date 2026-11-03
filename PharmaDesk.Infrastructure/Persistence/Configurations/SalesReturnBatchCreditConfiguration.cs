using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Domain.Returns;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class SalesReturnBatchCreditConfiguration : IEntityTypeConfiguration<SalesReturnBatchCredit>
{
    public void Configure(EntityTypeBuilder<SalesReturnBatchCredit> builder)
    {
        builder.ToTable("SalesReturnBatchCredits", "dbo");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.QuantityCredited).HasColumnType("decimal(18,2)");

        builder.HasIndex(c => c.SalesReturnId);
        builder.HasIndex(c => c.MedicineBatchId);

        builder.HasOne<SalesReturn>().WithMany().HasForeignKey(c => c.SalesReturnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MedicineBatch>().WithMany().HasForeignKey(c => c.MedicineBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
