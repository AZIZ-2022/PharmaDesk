using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines", "dbo");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.BrandName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.GenericName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Strength).HasMaxLength(50);
        builder.Property(m => m.BarcodeNumber).HasMaxLength(100);
        builder.Property(m => m.RegistrationNumber).HasMaxLength(100);
        builder.Property(m => m.PackSize).HasColumnType("decimal(18,2)");
        builder.Property(m => m.ReorderLevel).HasColumnType("decimal(18,2)");
        builder.Property(m => m.VatRate).HasColumnType("decimal(18,2)");
        builder.Property(m => m.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(m => m.ModifiedAtUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.GenericName);
        builder.HasIndex(m => m.BrandName);

        builder.HasOne<Manufacturer>().WithMany().HasForeignKey(m => m.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MedicineCategory>().WithMany().HasForeignKey(m => m.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DosageForm>().WithMany().HasForeignKey(m => m.DosageFormId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(m => m.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(m => m.PackUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
