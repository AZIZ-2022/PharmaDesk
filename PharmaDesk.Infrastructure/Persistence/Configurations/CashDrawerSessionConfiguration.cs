using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Finance;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class CashDrawerSessionConfiguration : IEntityTypeConfiguration<CashDrawerSession>
{
    public void Configure(EntityTypeBuilder<CashDrawerSession> builder)
    {
        builder.ToTable("CashDrawerSessions", "dbo");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OpeningCashAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ClosingCashAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ExpectedClosingCashAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Variance).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.OpenedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(s => s.ClosedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.BranchId);
    }
}
