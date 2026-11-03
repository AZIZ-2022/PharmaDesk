namespace PharmaDesk.Web.Models.Purchasing;

public class PurchaseListItemViewModel
{
    public long Id { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Due => TotalAmount - AmountPaid;
}
