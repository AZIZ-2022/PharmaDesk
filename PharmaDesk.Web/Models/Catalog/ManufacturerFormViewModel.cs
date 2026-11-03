using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Catalog;

public class ManufacturerFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Manufacturer name")]
    public string Name { get; set; } = string.Empty;

    public string? Country { get; set; }

    public bool IsEditMode { get; set; }
}
