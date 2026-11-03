namespace PharmaDesk.Domain.Returns;

public class SalesReturn
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long SaleId { get; set; }

    public long SaleItemId { get; set; }

    public long CustomerId { get; set; }

    public decimal QuantityReturned { get; set; }

    public decimal UnitPriceAtSale { get; set; }

    public decimal ReturnAmount { get; set; }

    public bool IsRestocked { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime ReturnedAtUtc { get; set; }
}
