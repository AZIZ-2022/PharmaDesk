using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Catalog;

public class MedicineCategoryFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEditMode { get; set; }
}
