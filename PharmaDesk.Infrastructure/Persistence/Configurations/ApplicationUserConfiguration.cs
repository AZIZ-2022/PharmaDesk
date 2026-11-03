using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaDesk.Infrastructure.Identity;

namespace PharmaDesk.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.MustChangePassword)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnType("datetime2(0)");

        builder.Property(u => u.ModifiedAtUtc)
            .HasColumnType("datetime2(0)");
    }
}
