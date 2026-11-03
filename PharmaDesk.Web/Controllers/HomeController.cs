using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDatabaseHealthService _databaseHealthService;
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IDatabaseHealthService databaseHealthService,
        PharmaDeskDbContext dbContext,
        ICurrentTenantContext currentTenant,
        ILogger<HomeController> logger)
    {
        _databaseHealthService = databaseHealthService;
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<IActionResult> Index(long? branchId, CancellationToken cancellationToken)
    {
        var health = await _databaseHealthService.CheckAsync(cancellationToken);

        var model = new DashboardViewModel
        {
            IsDatabaseConnected = health.IsConnected,
            CurrentSchemaVersion = health.CurrentSchemaVersion,
            DatabaseErrorMessage = health.ErrorMessage,
        };

        if (!health.IsConnected)
        {
            return View(model);
        }

        var effectiveBranchId = branchId ?? _currentTenant.BranchId;
        model.BranchId = effectiveBranchId;

        model.AvailableBranches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var monthStart = new DateOnly(today.Year, today.Month, 1).ToDateTime(TimeOnly.MinValue);
        var chartStart = today.AddDays(-13).ToDateTime(TimeOnly.MinValue);

        var salesQuery = _dbContext.Sales.AsQueryable();
        if (effectiveBranchId.HasValue)
        {
            salesQuery = salesQuery.Where(s => s.BranchId == effectiveBranchId.Value);
        }

        model.TodayRevenue = await salesQuery
            .Where(s => s.SaleDate >= todayStart)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        model.MonthRevenue = await salesQuery
            .Where(s => s.SaleDate >= monthStart)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        model.TotalReceivable = await _dbContext.Customers
            .Where(c => c.CurrentBalance > 0)
            .SumAsync(c => (decimal?)c.CurrentBalance, cancellationToken) ?? 0m;

        model.TotalPayable = await _dbContext.Suppliers
            .Where(s => s.CurrentBalance > 0)
            .SumAsync(s => (decimal?)s.CurrentBalance, cancellationToken) ?? 0m;

        var batchQuery = _dbContext.MedicineBatches.AsQueryable();
        if (effectiveBranchId.HasValue)
        {
            batchQuery = batchQuery.Where(b => b.BranchId == effectiveBranchId.Value);
        }

        var batchTotals = await batchQuery
            .GroupBy(b => b.MedicineId)
            .Select(g => new { MedicineId = g.Key, Total = g.Sum(b => b.QuantityRemaining) })
            .ToDictionaryAsync(x => x.MedicineId, x => x.Total, cancellationToken);

        var medicines = await _dbContext.Medicines
            .Where(m => m.IsActive)
            .Select(m => new { m.Id, m.ReorderLevel })
            .ToListAsync(cancellationToken);

        model.LowStockCount = medicines.Count(m => (batchTotals.TryGetValue(m.Id, out var qty) ? qty : 0) <= m.ReorderLevel);

        model.ExpiringSoonCount = await batchQuery
            .CountAsync(b => b.QuantityRemaining > 0 && b.ExpiryDate >= today && b.ExpiryDate <= today.AddDays(30), cancellationToken);

        var dailySales = await salesQuery
            .Where(s => s.SaleDate >= chartStart)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) })
            .ToDictionaryAsync(x => x.Date, x => x.Total, cancellationToken);

        for (var day = chartStart; day <= todayStart; day = day.AddDays(1))
        {
            model.ChartLabels.Add(day.ToString("MMM d"));
            model.ChartValues.Add(dailySales.TryGetValue(day, out var total) ? total : 0m);
        }

        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
