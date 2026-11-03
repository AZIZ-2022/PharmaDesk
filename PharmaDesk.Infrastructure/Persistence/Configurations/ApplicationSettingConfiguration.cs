using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Domain.SystemEntities;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class ApplicationSettingConfiguration : IEntityTypeConfiguration<ApplicationSetting>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.ToTable("ApplicationSettings", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SettingKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.SettingKey).IsUnique();

        builder.Property(x => x.SettingValue)
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.ModifiedAtUtc)
            .HasColumnType("datetime2(0)");
    }
}
