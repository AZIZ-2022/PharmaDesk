namespace PharmaDesk.Web.Models.Finance;

public class CashDrawerIndexViewModel
{
    public List<CashDrawerBranchStatusViewModel> BranchStatuses { get; set; } = new();

    public List<CashDrawerHistoryItemViewModel> History { get; set; } = new();
}
