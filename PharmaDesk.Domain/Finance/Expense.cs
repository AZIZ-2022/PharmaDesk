namespace PharmaDesk.Domain.Finance;

public class Expense
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public long ExpenseCategoryId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime ExpenseDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
}
