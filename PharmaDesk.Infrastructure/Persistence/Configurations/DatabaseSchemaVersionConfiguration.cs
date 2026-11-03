using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.SystemEntities;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class DatabaseSchemaVersionConfiguration : IEntityTypeConfiguration<DatabaseSchemaVersion>
{
    public void Configure(EntityTypeBuilder<DatabaseSchemaVersion> builder)
    {
        builder.ToTable("DatabaseSchemaVersions", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VersionNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.VersionNumber).IsUnique();

        builder.Property(x => x.ModuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.AppliedAtUtc)
            .HasColumnType("datetime2(0)");
    }
}
