using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.Catalog;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class DosageFormConfiguration : IEntityTypeConfiguration<DosageForm>
{
    public void Configure(EntityTypeBuilder<DosageForm> builder)
    {
        builder.ToTable("DosageForms", "dbo");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(50).IsRequired();
    }
}
