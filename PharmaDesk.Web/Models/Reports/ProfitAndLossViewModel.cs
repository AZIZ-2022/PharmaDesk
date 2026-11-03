using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Reports;

public class ProfitAndLossViewModel
{
    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public long? BranchId { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public decimal Revenue { get; set; }

    public decimal Cogs { get; set; }

    public decimal GrossProfit { get; set; }

    public decimal Expenses { get; set; }

    public decimal NetProfit { get; set; }
}
