namespace PharmaDesk.Domain.Sales;

public class Sale
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long CustomerId { get; set; }

    public DateTime SaleDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
