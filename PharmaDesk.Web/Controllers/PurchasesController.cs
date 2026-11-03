using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Purchasing;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Purchasing;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class PurchasesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public PurchasesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Purchases.View)]
    public async Task<IActionResult> Index()
    {
        var purchases = await _dbContext.Purchases
            .Join(_dbContext.Suppliers, p => p.SupplierId, s => s.Id, (p, s) => new { p, s })
            .Join(_dbContext.Branches, x => x.p.BranchId, b => b.Id, (x, b) => new PurchaseListItemViewModel
            {
                Id = x.p.Id,
                SupplierName = x.s.Name,
                BranchName = b.Name,
                PurchaseDate = x.p.PurchaseDate,
                InvoiceNumber = x.p.InvoiceNumber,
                TotalAmount = x.p.TotalAmount,
                AmountPaid = x.p.AmountPaid,
            })
            .OrderByDescending(p => p.PurchaseDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        return View(purchases);
    }

    [PermissionAuthorize(PermissionCodes.Purchases.View)]
    public async Task<IActionResult> Details(long id)
    {
        var purchase = await _dbContext.Purchases.FindAsync(id);
        if (purchase is null)
        {
            return NotFound();
        }

        var supplier = await _dbContext.Suppliers.FindAsync(purchase.SupplierId);
        var branch = await _dbContext.Branches.FindAsync(purchase.BranchId);

        var items = await _dbContext.PurchaseItems
            .Where(i => i.PurchaseId == id)
            .Join(_dbContext.Medicines, i => i.MedicineId, m => m.Id, (i, m) => new { i, m })
            .Join(_dbContext.MedicineBatches, x => x.i.Id, b => b.PurchaseItemId, (x, b) => new PurchaseDetailsItemViewModel
            {
                MedicineBatchId = b.Id,
                MedicineBrandName = x.m.BrandName,
                BatchNumber = x.i.BatchNumber,
                ExpiryDate = x.i.ExpiryDate,
                QuantityPacks = x.i.QuantityPacks,
                PurchasePricePerPack = x.i.PurchasePricePerPack,
                SellingPricePerBaseUnit = x.i.SellingPricePerBaseUnit,
                LineTotal = x.i.LineTotal,
                BatchQuantityRemaining = b.QuantityRemaining,
            })
            .ToListAsync();

        var model = new PurchaseDetailsViewModel
        {
            Id = purchase.Id,
            SupplierName = supplier?.Name ?? string.Empty,
            BranchName = branch?.Name ?? string.Empty,
            PurchaseDate = purchase.PurchaseDate,
            InvoiceNumber = purchase.InvoiceNumber,
            SubTotal = purchase.SubTotal,
            DiscountAmount = purchase.DiscountAmount,
            TaxAmount = purchase.TaxAmount,
            TotalAmount = purchase.TotalAmount,
            AmountPaid = purchase.AmountPaid,
            Items = items,
        };

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Purchases.Manage)]
    public async Task<IActionResult> Create()
    {
        var model = new PurchaseFormViewModel();
        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Purchases.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseFormViewModel model)
    {
        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Add at least one line item.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var medicineIds = model.Items.Select(i => i.MedicineId).Distinct().ToList();
        var medicines = await _dbContext.Medicines
            .Where(m => medicineIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        if (medicines.Count != medicineIds.Count)
        {
            ModelState.AddModelError(string.Empty, "One or more selected medicines could not be found.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var supplier = await _dbContext.Suppliers.FindAsync(model.SupplierId);
        if (supplier is null)
        {
            ModelState.AddModelError(string.Empty, "Selected supplier could not be found.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var subTotal = model.Items.Sum(i => i.QuantityPacks * i.PurchasePricePerPack);
        var totalAmount = subTotal - model.DiscountAmount + model.TaxAmount;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var purchase = new Purchase
        {
            TenantId = tenantId,
            BranchId = model.BranchId,
            SupplierId = model.SupplierId,
            PurchaseDate = model.PurchaseDate.ToDateTime(TimeOnly.MinValue),
            InvoiceNumber = model.InvoiceNumber,
            SubTotal = subTotal,
            DiscountAmount = model.DiscountAmount,
            TaxAmount = model.TaxAmount,
            TotalAmount = totalAmount,
            AmountPaid = model.AmountPaid,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Purchases.Add(purchase);
        await _dbContext.SaveChangesAsync();

        foreach (var itemModel in model.Items)
        {
            var medicine = medicines[itemModel.MedicineId];

            var purchaseItem = new PurchaseItem
            {
                PurchaseId = purchase.Id,
                MedicineId = medicine.Id,
                BatchNumber = itemModel.BatchNumber,
                ExpiryDate = itemModel.ExpiryDate,
                QuantityPacks = itemModel.QuantityPacks,
                PurchasePricePerPack = itemModel.PurchasePricePerPack,
                SellingPricePerBaseUnit = itemModel.SellingPricePerBaseUnit,
                LineTotal = itemModel.QuantityPacks * itemModel.PurchasePricePerPack,
            };

            _dbContext.PurchaseItems.Add(purchaseItem);
            await _dbContext.SaveChangesAsync();

            var quantityReceived = PurchaseBatchCalculator.ConvertPacksToBaseUnits(itemModel.QuantityPacks, medicine.PackSize);
            var costPerBaseUnit = PurchaseBatchCalculator.CalculateCostPerBaseUnit(itemModel.PurchasePricePerPack, medicine.PackSize);

            var batch = new MedicineBatch
            {
                TenantId = tenantId,
                BranchId = model.BranchId,
                MedicineId = medicine.Id,
                SupplierId = model.SupplierId,
                PurchaseId = purchase.Id,
                PurchaseItemId = purchaseItem.Id,
                BatchNumber = itemModel.BatchNumber,
                ExpiryDate = itemModel.ExpiryDate,
                QuantityReceived = quantityReceived,
                QuantityRemaining = quantityReceived,
                PurchaseCostPerBaseUnit = costPerBaseUnit,
                SellingPricePerBaseUnit = itemModel.SellingPricePerBaseUnit,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _dbContext.MedicineBatches.Add(batch);
        }

        supplier.CurrentBalance += totalAmount - model.AmountPaid;
        supplier.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("PurchaseCreated", "Purchase", purchase.Id.ToString(), $"Recorded purchase of {totalAmount:N2} from {supplier.Name}");

        return RedirectToAction(nameof(Details), new { id = purchase.Id });
    }

    private async Task PopulateDropdownsAsync(PurchaseFormViewModel model)
    {
        model.AvailableSuppliers = await _dbContext.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToListAsync();

        model.AvailableBranches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        model.AvailableMedicines = await _dbContext.Medicines
            .Where(m => m.IsActive)
            .Join(_dbContext.UnitsOfMeasure, m => m.PackUnitId, u => u.Id, (m, packUnit) => new { m, packUnit })
            .Join(_dbContext.UnitsOfMeasure, x => x.m.BaseUnitId, u => u.Id, (x, baseUnit) => new MedicineOptionViewModel
            {
                Id = x.m.Id,
                BrandName = x.m.BrandName,
                PackUnitName = x.packUnit.Name,
                PackSize = x.m.PackSize,
                BaseUnitName = baseUnit.Name,
            })
            .OrderBy(m => m.BrandName)
            .ToListAsync();
    }
}
