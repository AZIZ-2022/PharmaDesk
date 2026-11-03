using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Roles;

public class RoleEditViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Role name")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEditMode { get; set; }

    public List<PermissionCheckboxViewModel> Permissions { get; set; } = new();
}

public class PermissionCheckboxViewModel
{
    public int PermissionId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public bool IsGranted { get; set; }
}
