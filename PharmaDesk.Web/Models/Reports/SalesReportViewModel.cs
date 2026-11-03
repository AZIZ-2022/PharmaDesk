using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Reports;

public class SalesReportViewModel
{
    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public long? BranchId { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public List<SalesReportRowViewModel> Rows { get; set; } = new();

    public decimal TotalRevenue => Rows.Sum(r => r.Revenue);
}

public class SalesReportRowViewModel
{
    public string MedicineBrandName { get; set; } = string.Empty;

    public string BaseUnitName { get; set; } = string.Empty;

    public decimal QuantitySold { get; set; }

    public decimal Revenue { get; set; }
}
