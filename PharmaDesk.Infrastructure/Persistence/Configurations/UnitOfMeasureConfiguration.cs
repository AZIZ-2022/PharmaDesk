using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitsOfMeasure", "dbo");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(50).IsRequired();
        builder.Property(u => u.ShortCode).HasMaxLength(10).IsRequired();
    }
}
