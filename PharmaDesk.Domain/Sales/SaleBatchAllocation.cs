namespace PharmaDesk.Domain.Sales;

public class SaleBatchAllocation
{
    public long Id { get; set; }

    public long SaleItemId { get; set; }

    public long MedicineBatchId { get; set; }

    public decimal QuantityAllocated { get; set; }

    public decimal CostPerBaseUnitAtSale { get; set; }
}
