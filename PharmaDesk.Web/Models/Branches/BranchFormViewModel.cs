using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Branches;

public class BranchFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Branch name")]
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public bool IsEditMode { get; set; }
}
