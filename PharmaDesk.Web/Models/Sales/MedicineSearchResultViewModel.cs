namespace PharmaDesk.Web.Models.Sales;

public class MedicineSearchResultViewModel
{
    public long MedicineId { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public string BaseUnitName { get; set; } = string.Empty;

    public decimal? UnitPrice { get; set; }

    public decimal AvailableQuantity { get; set; }
}
