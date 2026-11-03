using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Finance;

public class ExpenseCategoryFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEditMode { get; set; }
}
