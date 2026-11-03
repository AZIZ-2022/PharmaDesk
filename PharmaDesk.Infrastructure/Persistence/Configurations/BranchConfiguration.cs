using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Organization;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "dbo");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.Phone).HasMaxLength(30);
        builder.Property(b => b.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(b => b.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(b => b.TenantId);
    }
}
