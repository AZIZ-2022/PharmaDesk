namespace PharmaDesk.Web.Models.Sales;

public class SaleDetailsViewModel
{
    public long Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal Due => TotalAmount - AmountPaid;

    public List<SaleDetailsItemViewModel> Items { get; set; } = new();
}

public class SaleDetailsItemViewModel
{
    public long SaleItemId { get; set; }

    public string MedicineBrandName { get; set; } = string.Empty;

    public decimal QuantityBaseUnits { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsReturned { get; set; }
}
