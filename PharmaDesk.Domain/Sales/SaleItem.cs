namespace PharmaDesk.Domain.Sales;

public class SaleItem
{
    public long Id { get; set; }

    public long SaleId { get; set; }

    public long MedicineId { get; set; }

    public decimal QuantityBaseUnits { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
