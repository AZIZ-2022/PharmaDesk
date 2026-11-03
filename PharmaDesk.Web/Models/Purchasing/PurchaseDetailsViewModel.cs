namespace PharmaDesk.Web.Models.Purchasing;

public class PurchaseDetailsViewModel
{
    public long Id { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Due => TotalAmount - AmountPaid;

    public List<PurchaseDetailsItemViewModel> Items { get; set; } = new();
}

public class PurchaseDetailsItemViewModel
{
    public long MedicineBatchId { get; set; }

    public string MedicineBrandName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public decimal QuantityPacks { get; set; }

    public decimal PurchasePricePerPack { get; set; }

    public decimal SellingPricePerBaseUnit { get; set; }

    public decimal LineTotal { get; set; }

    public decimal BatchQuantityRemaining { get; set; }
}
