using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Reports;

public class PurchaseReportViewModel
{
    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public long? BranchId { get; set; }

    public long? SupplierId { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public List<SelectListItem> AvailableSuppliers { get; set; } = new();

    public List<PurchaseReportRowViewModel> Rows { get; set; } = new();

    public decimal TotalCost => Rows.Sum(r => r.TotalCost);
}

public class PurchaseReportRowViewModel
{
    public string MedicineBrandName { get; set; } = string.Empty;

    public string PackUnitName { get; set; } = string.Empty;

    public decimal QuantityPacks { get; set; }

    public decimal TotalCost { get; set; }
}
