using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class MedicineCategoryConfiguration : IEntityTypeConfiguration<MedicineCategory>
{
    public void Configure(EntityTypeBuilder<MedicineCategory> builder)
    {
        builder.ToTable("MedicineCategories", "dbo");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(c => c.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(c => c.TenantId);
    }
}
