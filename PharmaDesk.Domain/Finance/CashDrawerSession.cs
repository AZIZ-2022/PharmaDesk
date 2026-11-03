namespace PharmaDesk.Domain.Finance;

public class CashDrawerSession
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public long BranchId { get; set; }

    public DateOnly SessionDate { get; set; }

    public decimal OpeningCashAmount { get; set; }

    public decimal? ClosingCashAmount { get; set; }

    public decimal? ExpectedClosingCashAmount { get; set; }

    public decimal? Variance { get; set; }

    public string? Notes { get; set; }

    public DateTime OpenedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }
}
