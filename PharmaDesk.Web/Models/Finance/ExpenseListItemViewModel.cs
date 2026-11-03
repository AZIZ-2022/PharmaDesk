namespace PharmaDesk.Web.Models.Finance;

public class ExpenseListItemViewModel
{
    public long Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime ExpenseDate { get; set; }

    public bool IsActive { get; set; }
}
