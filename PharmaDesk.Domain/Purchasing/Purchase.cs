namespace PharmaDesk.Domain.Purchasing;

public class Purchase
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long SupplierId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
