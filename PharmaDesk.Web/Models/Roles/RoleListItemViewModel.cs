namespace PharmaDesk.Web.Models.Roles;

public class RoleListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int PermissionCount { get; set; }
}
