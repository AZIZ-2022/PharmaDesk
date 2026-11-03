namespace PharmaDesk.Web.Models.Returns;

public class SalesReturnListItemViewModel
{
    public long Id { get; set; }

    public long SaleId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string MedicineBrandName { get; set; } = string.Empty;

    public decimal QuantityReturned { get; set; }

    public decimal ReturnAmount { get; set; }

    public bool IsRestocked { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime ReturnedAtUtc { get; set; }
}
