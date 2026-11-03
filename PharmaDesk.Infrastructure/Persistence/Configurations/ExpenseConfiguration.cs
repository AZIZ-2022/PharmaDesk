using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Finance;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses", "dbo");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PaymentMethod).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.ExpenseDate).HasColumnType("datetime2(0)");
        builder.Property(e => e.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(e => e.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.ExpenseDate);

        builder.HasOne<ExpenseCategory>().WithMany().HasForeignKey(e => e.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
