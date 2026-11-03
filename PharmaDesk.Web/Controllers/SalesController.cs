using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Sales;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Sales;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Sales;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class SalesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public SalesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Sales.View)]
    public async Task<IActionResult> Index()
    {
        var sales = await _dbContext.Sales
            .Join(_dbContext.Customers, s => s.CustomerId, c => c.Id, (s, c) => new { s, c })
            .Join(_dbContext.Branches, x => x.s.BranchId, b => b.Id, (x, b) => new SaleListItemViewModel
            {
                Id = x.s.Id,
                CustomerName = x.c.Name,
                BranchName = b.Name,
                SaleDate = x.s.SaleDate,
                TotalAmount = x.s.TotalAmount,
                AmountPaid = x.s.AmountPaid,
            })
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.Id)
            .ToListAsync();

        return View(sales);
    }

    [PermissionAuthorize(PermissionCodes.Sales.View)]
    public async Task<IActionResult> Details(long id)
    {
        var sale = await _dbContext.Sales.FindAsync(id);
        if (sale is null)
        {
            return NotFound();
        }

        var customer = await _dbContext.Customers.FindAsync(sale.CustomerId);
        var branch = await _dbContext.Branches.FindAsync(sale.BranchId);

        var returnedSaleItemIds = await _dbContext.SalesReturns
            .Where(r => r.SaleId == id)
            .Select(r => r.SaleItemId)
            .ToListAsync();

        var items = await _dbContext.SaleItems
            .Where(i => i.SaleId == id)
            .Join(_dbContext.Medicines, i => i.MedicineId, m => m.Id, (i, m) => new SaleDetailsItemViewModel
            {
                SaleItemId = i.Id,
                MedicineBrandName = m.BrandName,
                QuantityBaseUnits = i.QuantityBaseUnits,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
            })
            .ToListAsync();

        foreach (var item in items)
        {
            item.IsReturned = returnedSaleItemIds.Contains(item.SaleItemId);
        }

        var model = new SaleDetailsViewModel
        {
            Id = sale.Id,
            CustomerName = customer?.Name ?? string.Empty,
            BranchName = branch?.Name ?? string.Empty,
            SaleDate = sale.SaleDate,
            SubTotal = sale.SubTotal,
            DiscountAmount = sale.DiscountAmount,
            TaxAmount = sale.TaxAmount,
            TotalAmount = sale.TotalAmount,
            AmountPaid = sale.AmountPaid,
            PaymentMethod = sale.PaymentMethod,
            Items = items,
        };

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Sales.Manage)]
    public async Task<IActionResult> Pos()
    {
        var model = new SaleFormViewModel
        {
            BranchId = _currentTenant.BranchId ?? 0,
        };

        var walkIn = await _dbContext.Customers.FirstOrDefaultAsync(c => c.IsWalkInDefault);
        if (walkIn is not null)
        {
            model.CustomerId = walkIn.Id;
        }

        await PopulateDropdownsAsync(model);

        if (model.BranchId == 0 && model.AvailableBranches.Count > 0)
        {
            model.BranchId = long.Parse(model.AvailableBranches[0].Value!);
        }

        return View(model);
    }

    [HttpGet]
    [PermissionAuthorize(PermissionCodes.Sales.Manage)]
    public async Task<IActionResult> SearchMedicines(string q, long branchId)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(Array.Empty<MedicineSearchResultViewModel>());
        }

        var medicines = await _dbContext.Medicines
            .Where(m => m.IsActive && (m.BrandName.Contains(q) || m.GenericName.Contains(q)))
            .OrderBy(m => m.BrandName)
            .Take(20)
            .Select(m => new { m.Id, m.BrandName, m.GenericName, m.BaseUnitId })
            .ToListAsync();

        var medicineIds = medicines.Select(m => m.Id).ToList();

        var batches = await _dbContext.MedicineBatches
            .Where(b => medicineIds.Contains(b.MedicineId) && b.BranchId == branchId && b.QuantityRemaining > 0)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new { b.Id, b.MedicineId, b.QuantityRemaining, b.PurchaseCostPerBaseUnit, b.SellingPricePerBaseUnit, b.ExpiryDate })
            .ToListAsync();

        var baseUnitIds = medicines.Select(m => m.BaseUnitId).Distinct().ToList();
        var unitNames = await _dbContext.UnitsOfMeasure
            .Where(u => baseUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var results = medicines.Select(m =>
        {
            var medicineBatches = batches
                .Where(b => b.MedicineId == m.Id)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new BatchStockSnapshot(b.Id, b.QuantityRemaining, b.PurchaseCostPerBaseUnit, b.SellingPricePerBaseUnit, b.ExpiryDate))
                .ToList();

            return new MedicineSearchResultViewModel
            {
                MedicineId = m.Id,
                BrandName = m.BrandName,
                GenericName = m.GenericName,
                BaseUnitName = unitNames.TryGetValue(m.BaseUnitId, out var name) ? name : string.Empty,
                UnitPrice = FefoAllocator.GetQuotedUnitPrice(medicineBatches),
                AvailableQuantity = medicineBatches.Sum(b => b.QuantityRemaining),
            };
        }).ToList();

        return Json(results);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Sales.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(SaleFormViewModel model)
    {
        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Add at least one item.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(nameof(Pos), model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var customer = await _dbContext.Customers.FindAsync(model.CustomerId);
        if (customer is null)
        {
            ModelState.AddModelError(string.Empty, "Selected customer could not be found.");
            await PopulateDropdownsAsync(model);
            return View(nameof(Pos), model);
        }

        var medicineIds = model.Items.Select(i => i.MedicineId).Distinct().ToList();
        var medicines = await _dbContext.Medicines
            .Where(m => medicineIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        if (medicines.Count != medicineIds.Count)
        {
            ModelState.AddModelError(string.Empty, "One or more selected medicines could not be found.");
            await PopulateDropdownsAsync(model);
            return View(nameof(Pos), model);
        }

        // Resolve FEFO allocation and the authoritative price per line before writing
        // anything - never trust the client-posted UnitPriceQuoted for money math.
        var lineComputations = new List<(SaleItemFormViewModel ItemModel, decimal UnitPrice, List<BatchAllocationResult> Allocations)>();

        foreach (var itemModel in model.Items)
        {
            var batchSnapshots = await GetOrderedBatchSnapshotsAsync(itemModel.MedicineId, model.BranchId);
            var quotedPrice = FefoAllocator.GetQuotedUnitPrice(batchSnapshots);

            if (quotedPrice is null || !FefoAllocator.TryAllocate(batchSnapshots, itemModel.QuantityBaseUnits, out var allocations))
            {
                var medicineName = medicines[itemModel.MedicineId].BrandName;
                ModelState.AddModelError(string.Empty, $"Not enough stock of {medicineName} at the selected branch to fulfil this sale.");
                await PopulateDropdownsAsync(model);
                return View(nameof(Pos), model);
            }

            lineComputations.Add((itemModel, quotedPrice.Value, allocations));
        }

        var subTotal = lineComputations.Sum(l => l.UnitPrice * l.ItemModel.QuantityBaseUnits);
        var totalAmount = subTotal - model.DiscountAmount + model.TaxAmount;
        var due = totalAmount - model.AmountPaid;

        if (due > 0 && !customer.IsCreditEligible)
        {
            ModelState.AddModelError(nameof(model.AmountPaid), "This customer is not credit-eligible - the amount paid must cover the full total.");
            await PopulateDropdownsAsync(model);
            return View(nameof(Pos), model);
        }

        if (due > 0 && customer.IsCreditEligible && customer.CreditLimit.HasValue && customer.CurrentBalance + due > customer.CreditLimit.Value)
        {
            ModelState.AddModelError(nameof(model.AmountPaid), $"This sale would push {customer.Name}'s balance past their credit limit of {customer.CreditLimit.Value:N2}.");
            await PopulateDropdownsAsync(model);
            return View(nameof(Pos), model);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var sale = new Sale
        {
            TenantId = tenantId,
            BranchId = model.BranchId,
            CustomerId = model.CustomerId,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            DiscountAmount = model.DiscountAmount,
            TaxAmount = model.TaxAmount,
            TotalAmount = totalAmount,
            AmountPaid = model.AmountPaid,
            PaymentMethod = model.PaymentMethod,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Sales.Add(sale);
        await _dbContext.SaveChangesAsync();

        foreach (var (itemModel, unitPrice, allocations) in lineComputations)
        {
            var saleItem = new SaleItem
            {
                SaleId = sale.Id,
                MedicineId = itemModel.MedicineId,
                QuantityBaseUnits = itemModel.QuantityBaseUnits,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * itemModel.QuantityBaseUnits,
            };

            _dbContext.SaleItems.Add(saleItem);
            await _dbContext.SaveChangesAsync();

            foreach (var allocation in allocations)
            {
                var batch = await _dbContext.MedicineBatches.FindAsync(allocation.BatchId);
                if (batch is null)
                {
                    throw new InvalidOperationException($"Batch {allocation.BatchId} disappeared mid-transaction.");
                }

                batch.QuantityRemaining -= allocation.QuantityAllocated;

                _dbContext.SaleBatchAllocations.Add(new SaleBatchAllocation
                {
                    SaleItemId = saleItem.Id,
                    MedicineBatchId = allocation.BatchId,
                    QuantityAllocated = allocation.QuantityAllocated,
                    CostPerBaseUnitAtSale = allocation.CostPerBaseUnit,
                });
            }
        }

        customer.CurrentBalance += due;
        customer.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("SaleCompleted", "Sale", sale.Id.ToString(), $"Recorded sale of {totalAmount:N2} to {customer.Name}");

        return RedirectToAction(nameof(Details), new { id = sale.Id });
    }

    private async Task<List<BatchStockSnapshot>> GetOrderedBatchSnapshotsAsync(long medicineId, long branchId)
    {
        return await _dbContext.MedicineBatches
            .Where(b => b.MedicineId == medicineId && b.BranchId == branchId && b.QuantityRemaining > 0)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new BatchStockSnapshot(b.Id, b.QuantityRemaining, b.PurchaseCostPerBaseUnit, b.SellingPricePerBaseUnit, b.ExpiryDate))
            .ToListAsync();
    }

    private async Task PopulateDropdownsAsync(SaleFormViewModel model)
    {
        model.AvailableBranches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        model.AvailableCustomers = await _dbContext.Customers
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.IsWalkInDefault)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }
}
