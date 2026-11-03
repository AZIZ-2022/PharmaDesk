namespace PharmaDesk.Web.Models.Returns;

public class PurchaseReturnListItemViewModel
{
    public long Id { get; set; }

    public long PurchaseId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string MedicineBrandName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public decimal QuantityReturned { get; set; }

    public decimal ReturnAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime ReturnedAtUtc { get; set; }
}
