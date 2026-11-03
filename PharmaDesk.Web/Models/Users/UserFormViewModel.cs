using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Users;

public class UserFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Temporary password")]
    public string? Password { get; set; }

    [Display(Name = "Role")]
    public long? SelectedRoleId { get; set; }

    [Display(Name = "Branch")]
    public long? SelectedBranchId { get; set; }

    public bool IsEditMode { get; set; }

    public List<SelectListItem> AvailableRoles { get; set; } = new();

    public List<SelectListItem> AvailableBranches { get; set; } = new();
}
