using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Inventory;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Inventory;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private const int ExpiryAlertThresholdDays = 90;

    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public InventoryController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Inventory.View)]
    public async Task<IActionResult> StockOverview(long? branchId)
    {
        var batchQuery = _dbContext.MedicineBatches.AsQueryable();
        if (branchId.HasValue)
        {
            batchQuery = batchQuery.Where(b => b.BranchId == branchId.Value);
        }

        var batchTotals = await batchQuery
            .GroupBy(b => b.MedicineId)
            .Select(g => new { MedicineId = g.Key, Total = g.Sum(b => b.QuantityRemaining) })
            .ToDictionaryAsync(x => x.MedicineId, x => x.Total);

        var medicines = await _dbContext.Medicines
            .Where(m => m.IsActive)
            .OrderBy(m => m.BrandName)
            .ToListAsync();

        var items = medicines
            .Select(m => new StockOverviewItemViewModel
            {
                MedicineId = m.Id,
                BrandName = m.BrandName,
                GenericName = m.GenericName,
                QuantityRemaining = batchTotals.TryGetValue(m.Id, out var qty) ? qty : 0,
                ReorderLevel = m.ReorderLevel,
            })
            .ToList();

        await PopulateBranchFilterAsync(branchId);

        return View(items);
    }

    [PermissionAuthorize(PermissionCodes.Inventory.View)]
    public async Task<IActionResult> Batches(long? medicineId, long? branchId, bool showExhausted = false)
    {
        var query = _dbContext.MedicineBatches.AsQueryable();

        if (medicineId.HasValue)
        {
            query = query.Where(b => b.MedicineId == medicineId.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(b => b.BranchId == branchId.Value);
        }

        if (!showExhausted)
        {
            query = query.Where(b => b.QuantityRemaining > 0);
        }

        var batches = await ProjectToListItemsAsync(query.OrderBy(b => b.ExpiryDate));

        await PopulateBranchFilterAsync(branchId);
        ViewData["ShowExhausted"] = showExhausted;
        ViewData["MedicineId"] = medicineId;

        return View(batches);
    }

    [PermissionAuthorize(PermissionCodes.Inventory.View)]
    public async Task<IActionResult> ExpiryAlerts()
    {
        var thresholdDate = DateOnly.FromDateTime(DateTime.Today).AddDays(ExpiryAlertThresholdDays);

        var query = _dbContext.MedicineBatches
            .Where(b => b.QuantityRemaining > 0 && b.ExpiryDate <= thresholdDate)
            .OrderBy(b => b.ExpiryDate);

        var batches = await ProjectToListItemsAsync(query);

        ViewData["ThresholdDays"] = ExpiryAlertThresholdDays;

        return View(batches);
    }

    [PermissionAuthorize(PermissionCodes.Inventory.Adjust)]
    public async Task<IActionResult> Adjust(long batchId)
    {
        var batch = await _dbContext.MedicineBatches.FindAsync(batchId);
        if (batch is null)
        {
            return NotFound();
        }

        var medicine = await _dbContext.Medicines.FindAsync(batch.MedicineId);

        var model = new StockAdjustmentFormViewModel
        {
            MedicineBatchId = batch.Id,
            MedicineBrandName = medicine?.BrandName ?? string.Empty,
            BatchNumber = batch.BatchNumber,
            CurrentQuantityRemaining = batch.QuantityRemaining,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Inventory.Adjust)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentFormViewModel model)
    {
        var batch = await _dbContext.MedicineBatches.FindAsync(model.MedicineBatchId);
        if (batch is null)
        {
            return NotFound();
        }

        var medicine = await _dbContext.Medicines.FindAsync(batch.MedicineId);
        model.MedicineBrandName = medicine?.BrandName ?? string.Empty;
        model.BatchNumber = batch.BatchNumber;
        model.CurrentQuantityRemaining = batch.QuantityRemaining;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!StockAdjustmentCalculator.TryApplyAdjustment(batch.QuantityRemaining, model.QuantityDelta, out var newQuantity))
        {
            ModelState.AddModelError(nameof(model.QuantityDelta), $"This would make the remaining quantity negative (currently {batch.QuantityRemaining:N2}).");
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        batch.QuantityRemaining = newQuantity;

        var adjustment = new StockAdjustment
        {
            TenantId = tenantId,
            BranchId = batch.BranchId,
            MedicineBatchId = batch.Id,
            QuantityDelta = model.QuantityDelta,
            Reason = model.Reason,
            Notes = model.Notes,
            AdjustedAtUtc = DateTime.UtcNow,
        };

        _dbContext.StockAdjustments.Add(adjustment);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("StockAdjusted", "MedicineBatch", batch.Id.ToString(), $"Adjusted batch {batch.BatchNumber} by {model.QuantityDelta:N2} ({model.Reason})");

        TempData["StatusMessage"] = "Stock adjustment recorded.";
        return RedirectToAction(nameof(Batches));
    }

    private async Task<List<BatchListItemViewModel>> ProjectToListItemsAsync(IQueryable<MedicineBatch> query)
    {
        return await query
            .Join(_dbContext.Medicines, b => b.MedicineId, m => m.Id, (b, m) => new { b, m })
            .Join(_dbContext.Branches, x => x.b.BranchId, br => br.Id, (x, br) => new { x.b, x.m, br })
            .Join(_dbContext.Suppliers, x => x.b.SupplierId, s => s.Id, (x, s) => new BatchListItemViewModel
            {
                Id = x.b.Id,
                MedicineBrandName = x.m.BrandName,
                BranchName = x.br.Name,
                SupplierName = s.Name,
                BatchNumber = x.b.BatchNumber,
                ExpiryDate = x.b.ExpiryDate,
                QuantityRemaining = x.b.QuantityRemaining,
                PurchaseCostPerBaseUnit = x.b.PurchaseCostPerBaseUnit,
                SellingPricePerBaseUnit = x.b.SellingPricePerBaseUnit,
            })
            .ToListAsync();
    }

    private async Task PopulateBranchFilterAsync(long? selectedBranchId)
    {
        var branches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        ViewData["AvailableBranches"] = branches;
        ViewData["SelectedBranchId"] = selectedBranchId;
    }
}
