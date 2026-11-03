using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Reports;
using PharmaDesk.Application.Security;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Reports;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;

    public ReportsController(PharmaDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [PermissionAuthorize(PermissionCodes.Reports.View)]
    public async Task<IActionResult> SalesReport(DateOnly? fromDate, DateOnly? toDate, long? branchId, string? format)
    {
        var (from, to) = ResolveDateRange(fromDate, toDate);

        var query = _dbContext.Sales.Where(s => s.SaleDate >= from && s.SaleDate < to);
        if (branchId.HasValue)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        var rows = await query
            .Join(_dbContext.SaleItems, s => s.Id, i => i.SaleId, (s, i) => i)
            .Join(_dbContext.Medicines, i => i.MedicineId, m => m.Id, (i, m) => new { i, m })
            .Join(_dbContext.UnitsOfMeasure, x => x.m.BaseUnitId, u => u.Id, (x, u) => new { x.i, x.m, UnitName = u.Name })
            .GroupBy(x => new { x.m.Id, x.m.BrandName, x.UnitName })
            .Select(g => new SalesReportRowViewModel
            {
                MedicineBrandName = g.Key.BrandName,
                BaseUnitName = g.Key.UnitName,
                QuantitySold = g.Sum(x => x.i.QuantityBaseUnits),
                Revenue = g.Sum(x => x.i.LineTotal),
            })
            .OrderByDescending(r => r.Revenue)
            .ToListAsync();

        if (format == "csv")
        {
            var csv = BuildCsv(
                new[] { "Medicine", "Unit", "Quantity Sold", "Revenue" },
                rows.Select(r => new[] { r.MedicineBrandName, r.BaseUnitName, r.QuantitySold.ToString("N2"), r.Revenue.ToString("N2") }));
            return CsvFile(csv, "sales-report.csv");
        }

        var model = new SalesReportViewModel
        {
            FromDate = fromDate ?? DateOnly.FromDateTime(from),
            ToDate = toDate ?? DateOnly.FromDateTime(to.AddDays(-1)),
            BranchId = branchId,
            Rows = rows,
        };

        await PopulateBranchesAsync(model.AvailableBranches);

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Reports.View)]
    public async Task<IActionResult> PurchaseReport(DateOnly? fromDate, DateOnly? toDate, long? branchId, long? supplierId, string? format)
    {
        var (from, to) = ResolveDateRange(fromDate, toDate);

        var query = _dbContext.Purchases.Where(p => p.PurchaseDate >= from && p.PurchaseDate < to);
        if (branchId.HasValue)
        {
            query = query.Where(p => p.BranchId == branchId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        var rows = await query
            .Join(_dbContext.PurchaseItems, p => p.Id, i => i.PurchaseId, (p, i) => i)
            .Join(_dbContext.Medicines, i => i.MedicineId, m => m.Id, (i, m) => new { i, m })
            .Join(_dbContext.UnitsOfMeasure, x => x.m.PackUnitId, u => u.Id, (x, u) => new { x.i, x.m, UnitName = u.Name })
            .GroupBy(x => new { x.m.Id, x.m.BrandName, x.UnitName })
            .Select(g => new PurchaseReportRowViewModel
            {
                MedicineBrandName = g.Key.BrandName,
                PackUnitName = g.Key.UnitName,
                QuantityPacks = g.Sum(x => x.i.QuantityPacks),
                TotalCost = g.Sum(x => x.i.LineTotal),
            })
            .OrderByDescending(r => r.TotalCost)
            .ToListAsync();

        if (format == "csv")
        {
            var csv = BuildCsv(
                new[] { "Medicine", "Pack Unit", "Quantity Purchased", "Total Cost" },
                rows.Select(r => new[] { r.MedicineBrandName, r.PackUnitName, r.QuantityPacks.ToString("N2"), r.TotalCost.ToString("N2") }));
            return CsvFile(csv, "purchase-report.csv");
        }

        var model = new PurchaseReportViewModel
        {
            FromDate = fromDate ?? DateOnly.FromDateTime(from),
            ToDate = toDate ?? DateOnly.FromDateTime(to.AddDays(-1)),
            BranchId = branchId,
            SupplierId = supplierId,
            Rows = rows,
        };

        await PopulateBranchesAsync(model.AvailableBranches);
        model.AvailableSuppliers = await _dbContext.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToListAsync();

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Reports.View)]
    public async Task<IActionResult> ProfitAndLoss(DateOnly? fromDate, DateOnly? toDate, long? branchId)
    {
        var (from, to) = ResolveDateRange(fromDate, toDate);

        var salesQuery = _dbContext.Sales.Where(s => s.SaleDate >= from && s.SaleDate < to);
        if (branchId.HasValue)
        {
            salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
        }

        var saleIds = salesQuery.Select(s => s.Id);

        var revenue = await _dbContext.SaleItems
            .Where(i => saleIds.Contains(i.SaleId))
            .SumAsync(i => (decimal?)i.LineTotal) ?? 0m;

        var cogs = await _dbContext.SaleBatchAllocations
            .Join(_dbContext.SaleItems, a => a.SaleItemId, i => i.Id, (a, i) => new { a, i })
            .Where(x => saleIds.Contains(x.i.SaleId))
            .SumAsync(x => (decimal?)(x.a.QuantityAllocated * x.a.CostPerBaseUnitAtSale)) ?? 0m;

        var expensesQuery = _dbContext.Expenses.Where(e => e.IsActive && e.ExpenseDate >= from && e.ExpenseDate < to);
        if (branchId.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
        }

        var expenses = await expensesQuery.SumAsync(e => (decimal?)e.Amount) ?? 0m;

        var result = ProfitAndLossCalculator.Compute(revenue, cogs, expenses);

        var model = new ProfitAndLossViewModel
        {
            FromDate = fromDate ?? DateOnly.FromDateTime(from),
            ToDate = toDate ?? DateOnly.FromDateTime(to.AddDays(-1)),
            BranchId = branchId,
            Revenue = result.Revenue,
            Cogs = result.Cogs,
            GrossProfit = result.GrossProfit,
            Expenses = result.Expenses,
            NetProfit = result.NetProfit,
        };

        await PopulateBranchesAsync(model.AvailableBranches);

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Reports.View)]
    public async Task<IActionResult> InventoryValuation(long? branchId, string? format)
    {
        var query = _dbContext.MedicineBatches.Where(b => b.QuantityRemaining > 0);
        if (branchId.HasValue)
        {
            query = query.Where(b => b.BranchId == branchId.Value);
        }

        var rows = await query
            .Join(_dbContext.Medicines, b => b.MedicineId, m => m.Id, (b, m) => new { b, m })
            .Join(_dbContext.UnitsOfMeasure, x => x.m.BaseUnitId, u => u.Id, (x, u) => new { x.b, x.m, UnitName = u.Name })
            .GroupBy(x => new { x.m.Id, x.m.BrandName, x.UnitName })
            .Select(g => new InventoryValuationRowViewModel
            {
                MedicineBrandName = g.Key.BrandName,
                BaseUnitName = g.Key.UnitName,
                QuantityRemaining = g.Sum(x => x.b.QuantityRemaining),
                TotalValue = g.Sum(x => x.b.QuantityRemaining * x.b.PurchaseCostPerBaseUnit),
            })
            .OrderByDescending(r => r.TotalValue)
            .ToListAsync();

        if (format == "csv")
        {
            var csv = BuildCsv(
                new[] { "Medicine", "Unit", "Quantity Remaining", "Stock Value" },
                rows.Select(r => new[] { r.MedicineBrandName, r.BaseUnitName, r.QuantityRemaining.ToString("N2"), r.TotalValue.ToString("N2") }));
            return CsvFile(csv, "inventory-valuation.csv");
        }

        var model = new InventoryValuationViewModel
        {
            BranchId = branchId,
            Rows = rows,
        };

        await PopulateBranchesAsync(model.AvailableBranches);

        return View(model);
    }

    private static (DateTime From, DateTime To) ResolveDateRange(DateOnly? fromDate, DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var from = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        var to = toDate ?? today;

        return (from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MinValue).AddDays(1));
    }

    private async Task PopulateBranchesAsync(List<SelectListItem> target)
    {
        var branches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        target.AddRange(branches);
    }

    private static string BuildCsv(string[] headers, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', headers.Select(EscapeCsvField)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row.Select(EscapeCsvField)));
        }

        return builder.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }

    private FileContentResult CsvFile(string csv, string fileName)
    {
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }
}
