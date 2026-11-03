namespace PharmaDesk.Web.Models.Inventory;

public class BatchListItemViewModel
{
    public long Id { get; set; }

    public string MedicineBrandName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public decimal QuantityRemaining { get; set; }

    public decimal PurchaseCostPerBaseUnit { get; set; }

    public decimal SellingPricePerBaseUnit { get; set; }

    public bool IsExpired => ExpiryDate < DateOnly.FromDateTime(DateTime.Today);

    public int DaysUntilExpiry => ExpiryDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
}
