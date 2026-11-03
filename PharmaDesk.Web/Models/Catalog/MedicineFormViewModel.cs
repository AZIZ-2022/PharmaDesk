using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Catalog;

public class MedicineFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Brand name")]
    public string BrandName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Generic name")]
    public string GenericName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Manufacturer")]
    public long ManufacturerId { get; set; }

    [Required]
    [Display(Name = "Category")]
    public long CategoryId { get; set; }

    [Required]
    [Display(Name = "Dosage form")]
    public int DosageFormId { get; set; }

    [Display(Name = "Strength")]
    public string? Strength { get; set; }

    [Required]
    [Display(Name = "Base (selling) unit")]
    public int BaseUnitId { get; set; }

    [Required]
    [Display(Name = "Pack unit")]
    public int PackUnitId { get; set; }

    [Range(0.01, 100000)]
    [Display(Name = "Pack size (base units per pack)")]
    public decimal PackSize { get; set; } = 1m;

    [Range(0, 1000000)]
    [Display(Name = "Reorder level (base units)")]
    public decimal ReorderLevel { get; set; }

    [Display(Name = "Barcode")]
    public string? BarcodeNumber { get; set; }

    [Display(Name = "Registration number")]
    public string? RegistrationNumber { get; set; }

    [Display(Name = "VAT applicable")]
    public bool IsVatApplicable { get; set; }

    [Display(Name = "VAT rate (%)")]
    public decimal? VatRate { get; set; }

    public bool IsEditMode { get; set; }

    public List<SelectListItem> AvailableManufacturers { get; set; } = new();

    public List<SelectListItem> AvailableCategories { get; set; } = new();

    public List<SelectListItem> AvailableDosageForms { get; set; } = new();

    public List<SelectListItem> AvailableUnits { get; set; } = new();
}
