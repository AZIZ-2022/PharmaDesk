using Microsoft.AspNetCore.Identity;

namespace PharmaDesk.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<long>
{
    public string FullName { get; set; } = string.Empty;

    public long TenantId { get; set; }

    public long? BranchId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
}
