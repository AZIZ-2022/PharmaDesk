using PharmaDesk.Domain.Security;

namespace PharmaDesk.Infrastructure.Identity;

public class RolePermission
{
    public long RoleId { get; set; }

    public ApplicationRole? Role { get; set; }

    public int PermissionId { get; set; }

    public Permission? Permission { get; set; }
}
