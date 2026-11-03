namespace PharmaDesk.Web.Models.Catalog;

public class MedicineListItemViewModel
{
    public long Id { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public string? Strength { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string ManufacturerName { get; set; } = string.Empty;

    public string DosageFormName { get; set; } = string.Empty;

    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; }
}
