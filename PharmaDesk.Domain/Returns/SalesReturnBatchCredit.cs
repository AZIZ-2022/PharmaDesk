namespace PharmaDesk.Domain.Returns;

public class SalesReturnBatchCredit
{
    public long Id { get; set; }

    public long SalesReturnId { get; set; }

    public long MedicineBatchId { get; set; }

    public decimal QuantityCredited { get; set; }
}
