using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models;

public class DashboardViewModel
{
    public bool IsDatabaseConnected { get; set; }

    public string? CurrentSchemaVersion { get; set; }

    public string? DatabaseErrorMessage { get; set; }

    public long? BranchId { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public decimal TodayRevenue { get; set; }

    public decimal MonthRevenue { get; set; }

    public decimal TotalReceivable { get; set; }

    public decimal TotalPayable { get; set; }

    public int LowStockCount { get; set; }

    public int ExpiringSoonCount { get; set; }

    public List<string> ChartLabels { get; set; } = new();

    public List<decimal> ChartValues { get; set; } = new();
}
