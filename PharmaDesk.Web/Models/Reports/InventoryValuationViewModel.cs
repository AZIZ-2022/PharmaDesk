using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Reports;

public class InventoryValuationViewModel
{
    public long? BranchId { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public List<InventoryValuationRowViewModel> Rows { get; set; } = new();

    public decimal TotalValue => Rows.Sum(r => r.TotalValue);
}

public class InventoryValuationRowViewModel
{
    public string MedicineBrandName { get; set; } = string.Empty;

    public string BaseUnitName { get; set; } = string.Empty;

    public decimal QuantityRemaining { get; set; }

    public decimal TotalValue { get; set; }
}
