namespace PharmaDesk.Domain.Finance;

public class ExpenseCategory
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
}
