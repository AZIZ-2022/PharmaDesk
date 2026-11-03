namespace PharmaDesk.Web.Models.Inventory;

public class StockOverviewItemViewModel
{
    public long MedicineId { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public decimal QuantityRemaining { get; set; }

    public decimal ReorderLevel { get; set; }

    public string Status => QuantityRemaining <= 0
        ? "Out of Stock"
        : QuantityRemaining <= ReorderLevel
            ? "Low"
            : "OK";
}
