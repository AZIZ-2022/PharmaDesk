using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Returns;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Returns;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Returns;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class ReturnsController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public ReturnsController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Returns.View)]
    public async Task<IActionResult> SalesReturns()
    {
        var returns = await _dbContext.SalesReturns
            .Join(_dbContext.Customers, r => r.CustomerId, c => c.Id, (r, c) => new { r, c })
            .Join(_dbContext.SaleItems, x => x.r.SaleItemId, si => si.Id, (x, si) => new { x.r, x.c, si })
            .Join(_dbContext.Medicines, x => x.si.MedicineId, m => m.Id, (x, m) => new SalesReturnListItemViewModel
            {
                Id = x.r.Id,
                SaleId = x.r.SaleId,
                CustomerName = x.c.Name,
                MedicineBrandName = m.BrandName,
                QuantityReturned = x.r.QuantityReturned,
                ReturnAmount = x.r.ReturnAmount,
                IsRestocked = x.r.IsRestocked,
                Reason = x.r.Reason,
                ReturnedAtUtc = x.r.ReturnedAtUtc,
            })
            .OrderByDescending(r => r.ReturnedAtUtc)
            .ToListAsync();

        return View(returns);
    }

    [PermissionAuthorize(PermissionCodes.Returns.View)]
    public async Task<IActionResult> PurchaseReturns()
    {
        var returns = await _dbContext.PurchaseReturns
            .Join(_dbContext.Suppliers, r => r.SupplierId, s => s.Id, (r, s) => new { r, s })
            .Join(_dbContext.MedicineBatches, x => x.r.MedicineBatchId, b => b.Id, (x, b) => new { x.r, x.s, b })
            .Join(_dbContext.Medicines, x => x.b.MedicineId, m => m.Id, (x, m) => new PurchaseReturnListItemViewModel
            {
                Id = x.r.Id,
                PurchaseId = x.r.PurchaseId,
                SupplierName = x.s.Name,
                MedicineBrandName = m.BrandName,
                BatchNumber = x.b.BatchNumber,
                QuantityReturned = x.r.QuantityReturned,
                ReturnAmount = x.r.ReturnAmount,
                Reason = x.r.Reason,
                ReturnedAtUtc = x.r.ReturnedAtUtc,
            })
            .OrderByDescending(r => r.ReturnedAtUtc)
            .ToListAsync();

        return View(returns);
    }

    [PermissionAuthorize(PermissionCodes.Returns.Manage)]
    public async Task<IActionResult> NewSalesReturn(long saleItemId)
    {
        var saleItem = await _dbContext.SaleItems.FindAsync(saleItemId);
        if (saleItem is null)
        {
            return NotFound();
        }

        var alreadyReturned = await _dbContext.SalesReturns.AnyAsync(r => r.SaleItemId == saleItemId);
        if (alreadyReturned)
        {
            TempData["StatusMessage"] = "This sale line has already been returned.";
            return RedirectToAction("Details", "Sales", new { id = saleItem.SaleId });
        }

        var medicine = await _dbContext.Medicines.FindAsync(saleItem.MedicineId);

        var model = new SalesReturnFormViewModel
        {
            SaleItemId = saleItem.Id,
            SaleId = saleItem.SaleId,
            MedicineBrandName = medicine?.BrandName ?? string.Empty,
            OriginalQuantity = saleItem.QuantityBaseUnits,
            UnitPriceAtSale = saleItem.UnitPrice,
            QuantityReturned = saleItem.QuantityBaseUnits,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Returns.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewSalesReturn(SalesReturnFormViewModel model)
    {
        var saleItem = await _dbContext.SaleItems.FindAsync(model.SaleItemId);
        if (saleItem is null)
        {
            return NotFound();
        }

        model.SaleId = saleItem.SaleId;
        model.OriginalQuantity = saleItem.QuantityBaseUnits;
        model.UnitPriceAtSale = saleItem.UnitPrice;

        var medicine = await _dbContext.Medicines.FindAsync(saleItem.MedicineId);
        model.MedicineBrandName = medicine?.BrandName ?? string.Empty;

        var alreadyReturned = await _dbContext.SalesReturns.AnyAsync(r => r.SaleItemId == model.SaleItemId);
        if (alreadyReturned)
        {
            ModelState.AddModelError(string.Empty, "This sale line has already been returned.");
        }

        if (model.QuantityReturned > saleItem.QuantityBaseUnits)
        {
            ModelState.AddModelError(nameof(model.QuantityReturned), $"Cannot return more than the original quantity sold ({saleItem.QuantityBaseUnits:N2}).");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var sale = await _dbContext.Sales.FindAsync(saleItem.SaleId);
        if (sale is null)
        {
            return NotFound();
        }

        List<BatchCreditResult>? credits = null;

        if (model.IsRestocked)
        {
            var originalAllocations = await _dbContext.SaleBatchAllocations
                .Where(a => a.SaleItemId == saleItem.Id)
                .OrderBy(a => a.Id)
                .Select(a => new OriginalBatchAllocationSnapshot(a.MedicineBatchId, a.QuantityAllocated))
                .ToListAsync();

            if (!SalesReturnAllocator.TryAllocateReturn(originalAllocations, model.QuantityReturned, out credits))
            {
                ModelState.AddModelError(nameof(model.QuantityReturned), "Could not match the returned quantity against the original sale's batch allocations.");
                return View(model);
            }
        }

        var customer = await _dbContext.Customers.FindAsync(sale.CustomerId);
        if (customer is null)
        {
            return NotFound();
        }

        var returnAmount = model.QuantityReturned * saleItem.UnitPrice;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var salesReturn = new SalesReturn
        {
            TenantId = tenantId,
            BranchId = sale.BranchId,
            SaleId = sale.Id,
            SaleItemId = saleItem.Id,
            CustomerId = sale.CustomerId,
            QuantityReturned = model.QuantityReturned,
            UnitPriceAtSale = saleItem.UnitPrice,
            ReturnAmount = returnAmount,
            IsRestocked = model.IsRestocked,
            Reason = model.Reason,
            Notes = model.Notes,
            ReturnedAtUtc = DateTime.UtcNow,
        };

        _dbContext.SalesReturns.Add(salesReturn);
        await _dbContext.SaveChangesAsync();

        if (credits is not null)
        {
            foreach (var credit in credits)
            {
                var batch = await _dbContext.MedicineBatches.FindAsync(credit.BatchId);
                if (batch is null)
                {
                    throw new InvalidOperationException($"Batch {credit.BatchId} disappeared mid-transaction.");
                }

                batch.QuantityRemaining += credit.QuantityCredited;

                _dbContext.SalesReturnBatchCredits.Add(new SalesReturnBatchCredit
                {
                    SalesReturnId = salesReturn.Id,
                    MedicineBatchId = credit.BatchId,
                    QuantityCredited = credit.QuantityCredited,
                });
            }
        }

        customer.CurrentBalance -= returnAmount;
        customer.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("SalesReturnRecorded", "SalesReturn", salesReturn.Id.ToString(), $"Returned {model.QuantityReturned:N2} of {model.MedicineBrandName} from sale #{sale.Id}");

        return RedirectToAction(nameof(SalesReturns));
    }

    [PermissionAuthorize(PermissionCodes.Returns.Manage)]
    public async Task<IActionResult> NewPurchaseReturn(long medicineBatchId)
    {
        var batch = await _dbContext.MedicineBatches.FindAsync(medicineBatchId);
        if (batch is null)
        {
            return NotFound();
        }

        var medicine = await _dbContext.Medicines.FindAsync(batch.MedicineId);

        var model = new PurchaseReturnFormViewModel
        {
            MedicineBatchId = batch.Id,
            PurchaseId = batch.PurchaseId,
            MedicineBrandName = medicine?.BrandName ?? string.Empty,
            BatchNumber = batch.BatchNumber,
            CurrentQuantityRemaining = batch.QuantityRemaining,
            CostPerBaseUnit = batch.PurchaseCostPerBaseUnit,
            QuantityReturned = batch.QuantityRemaining,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Returns.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewPurchaseReturn(PurchaseReturnFormViewModel model)
    {
        var batch = await _dbContext.MedicineBatches.FindAsync(model.MedicineBatchId);
        if (batch is null)
        {
            return NotFound();
        }

        model.PurchaseId = batch.PurchaseId;
        model.CurrentQuantityRemaining = batch.QuantityRemaining;
        model.CostPerBaseUnit = batch.PurchaseCostPerBaseUnit;

        var medicine = await _dbContext.Medicines.FindAsync(batch.MedicineId);
        model.MedicineBrandName = medicine?.BrandName ?? string.Empty;
        model.BatchNumber = batch.BatchNumber;

        if (model.QuantityReturned > batch.QuantityRemaining)
        {
            ModelState.AddModelError(nameof(model.QuantityReturned), $"Cannot return more than what's currently in stock for this batch ({batch.QuantityRemaining:N2}).");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var supplier = await _dbContext.Suppliers.FindAsync(batch.SupplierId);
        if (supplier is null)
        {
            return NotFound();
        }

        var returnAmount = model.QuantityReturned * batch.PurchaseCostPerBaseUnit;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        batch.QuantityRemaining -= model.QuantityReturned;

        var purchaseReturn = new PurchaseReturn
        {
            TenantId = tenantId,
            BranchId = batch.BranchId,
            PurchaseId = batch.PurchaseId,
            MedicineBatchId = batch.Id,
            SupplierId = batch.SupplierId,
            QuantityReturned = model.QuantityReturned,
            CostPerBaseUnitAtPurchase = batch.PurchaseCostPerBaseUnit,
            ReturnAmount = returnAmount,
            Reason = model.Reason,
            Notes = model.Notes,
            ReturnedAtUtc = DateTime.UtcNow,
        };

        _dbContext.PurchaseReturns.Add(purchaseReturn);

        supplier.CurrentBalance -= returnAmount;
        supplier.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("PurchaseReturnRecorded", "PurchaseReturn", purchaseReturn.Id.ToString(), $"Returned {model.QuantityReturned:N2} of {model.MedicineBrandName} (batch {model.BatchNumber}) to {supplier.Name}");

        return RedirectToAction(nameof(PurchaseReturns));
    }
}
