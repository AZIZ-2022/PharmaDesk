namespace PharmaDesk.Web.Models.Finance;

public class CashDrawerBranchStatusViewModel
{
    public long BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public long? OpenSessionId { get; set; }

    public decimal? OpeningCashAmount { get; set; }

    public DateTime? OpenedAtUtc { get; set; }

    public bool IsOpen => OpenSessionId.HasValue;
}
