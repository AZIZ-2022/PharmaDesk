namespace PharmaDesk.Web.Models.Finance;

public class CashDrawerHistoryItemViewModel
{
    public long Id { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public DateOnly SessionDate { get; set; }

    public decimal OpeningCashAmount { get; set; }

    public decimal? ClosingCashAmount { get; set; }

    public decimal? ExpectedClosingCashAmount { get; set; }

    public decimal? Variance { get; set; }

    public DateTime OpenedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }
}
