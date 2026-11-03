using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Catalog;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class MedicinesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public MedicinesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Medicines.View)]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _dbContext.Medicines
            .Join(_dbContext.MedicineCategories, m => m.CategoryId, c => c.Id, (m, c) => new { m, c })
            .Join(_dbContext.Manufacturers, x => x.m.ManufacturerId, mf => mf.Id, (x, mf) => new { x.m, x.c, mf })
            .Join(_dbContext.DosageForms, x => x.m.DosageFormId, d => d.Id, (x, d) => new MedicineListItemViewModel
            {
                Id = x.m.Id,
                BrandName = x.m.BrandName,
                GenericName = x.m.GenericName,
                Strength = x.m.Strength,
                CategoryName = x.c.Name,
                ManufacturerName = x.mf.Name,
                DosageFormName = d.Name,
                ReorderLevel = x.m.ReorderLevel,
                IsActive = x.m.IsActive,
            });

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(m => m.BrandName.Contains(q) || m.GenericName.Contains(q));
        }

        var medicines = await query
            .OrderBy(m => m.BrandName)
            .ToListAsync();

        ViewData["SearchTerm"] = q;

        return View(medicines);
    }

    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    public async Task<IActionResult> Create()
    {
        var model = new MedicineFormViewModel { PackSize = 1m };
        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicineFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var medicine = new Medicine
        {
            TenantId = tenantId,
            BrandName = model.BrandName,
            GenericName = model.GenericName,
            ManufacturerId = model.ManufacturerId,
            CategoryId = model.CategoryId,
            DosageFormId = model.DosageFormId,
            Strength = model.Strength,
            BaseUnitId = model.BaseUnitId,
            PackUnitId = model.PackUnitId,
            PackSize = model.PackSize,
            ReorderLevel = model.ReorderLevel,
            BarcodeNumber = model.BarcodeNumber,
            RegistrationNumber = model.RegistrationNumber,
            IsVatApplicable = model.IsVatApplicable,
            VatRate = model.IsVatApplicable ? model.VatRate : null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Medicines.Add(medicine);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("MedicineCreated", "Medicine", medicine.Id.ToString(), $"Created medicine {medicine.BrandName}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var medicine = await _dbContext.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        var model = new MedicineFormViewModel
        {
            Id = medicine.Id,
            BrandName = medicine.BrandName,
            GenericName = medicine.GenericName,
            ManufacturerId = medicine.ManufacturerId,
            CategoryId = medicine.CategoryId,
            DosageFormId = medicine.DosageFormId,
            Strength = medicine.Strength,
            BaseUnitId = medicine.BaseUnitId,
            PackUnitId = medicine.PackUnitId,
            PackSize = medicine.PackSize,
            ReorderLevel = medicine.ReorderLevel,
            BarcodeNumber = medicine.BarcodeNumber,
            RegistrationNumber = medicine.RegistrationNumber,
            IsVatApplicable = medicine.IsVatApplicable,
            VatRate = medicine.VatRate,
            IsEditMode = true,
        };

        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, MedicineFormViewModel model)
    {
        model.IsEditMode = true;

        var medicine = await _dbContext.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        medicine.BrandName = model.BrandName;
        medicine.GenericName = model.GenericName;
        medicine.ManufacturerId = model.ManufacturerId;
        medicine.CategoryId = model.CategoryId;
        medicine.DosageFormId = model.DosageFormId;
        medicine.Strength = model.Strength;
        medicine.BaseUnitId = model.BaseUnitId;
        medicine.PackUnitId = model.PackUnitId;
        medicine.PackSize = model.PackSize;
        medicine.ReorderLevel = model.ReorderLevel;
        medicine.BarcodeNumber = model.BarcodeNumber;
        medicine.RegistrationNumber = model.RegistrationNumber;
        medicine.IsVatApplicable = model.IsVatApplicable;
        medicine.VatRate = model.IsVatApplicable ? model.VatRate : null;
        medicine.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("MedicineUpdated", "Medicine", medicine.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var medicine = await _dbContext.Medicines.FindAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        medicine.IsActive = !medicine.IsActive;
        medicine.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(medicine.IsActive ? "MedicineActivated" : "MedicineDeactivated", "Medicine", medicine.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(MedicineFormViewModel model)
    {
        model.AvailableManufacturers = await _dbContext.Manufacturers
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new SelectListItem(m.Name, m.Id.ToString()))
            .ToListAsync();

        model.AvailableCategories = await _dbContext.MedicineCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();

        model.AvailableDosageForms = await _dbContext.DosageForms
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()))
            .ToListAsync();

        model.AvailableUnits = await _dbContext.UnitsOfMeasure
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem(u.Name, u.Id.ToString()))
            .ToListAsync();
    }
}
