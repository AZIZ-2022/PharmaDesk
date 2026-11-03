namespace PharmaDesk.Web.Models.Sales;

public class SaleListItemViewModel
{
    public long Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Due => TotalAmount - AmountPaid;
}
