using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Catalog;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class ManufacturersController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public ManufacturersController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Medicines.View)]
    public async Task<IActionResult> Index()
    {
        var manufacturers = await _dbContext.Manufacturers
            .OrderBy(m => m.Name)
            .Select(m => new ManufacturerListItemViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Country = m.Country,
                IsActive = m.IsActive,
            })
            .ToListAsync();

        return View(manufacturers);
    }

    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    public IActionResult Create()
    {
        return View(new ManufacturerFormViewModel());
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManufacturerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var manufacturer = new Manufacturer
        {
            TenantId = tenantId,
            Name = model.Name,
            Country = model.Country,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Manufacturers.Add(manufacturer);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("ManufacturerCreated", "Manufacturer", manufacturer.Id.ToString(), $"Created manufacturer {manufacturer.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var manufacturer = await _dbContext.Manufacturers.FindAsync(id);
        if (manufacturer is null)
        {
            return NotFound();
        }

        var model = new ManufacturerFormViewModel
        {
            Id = manufacturer.Id,
            Name = manufacturer.Name,
            Country = manufacturer.Country,
            IsEditMode = true,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ManufacturerFormViewModel model)
    {
        model.IsEditMode = true;

        var manufacturer = await _dbContext.Manufacturers.FindAsync(id);
        if (manufacturer is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        manufacturer.Name = model.Name;
        manufacturer.Country = model.Country;
        manufacturer.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("ManufacturerUpdated", "Manufacturer", manufacturer.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var manufacturer = await _dbContext.Manufacturers.FindAsync(id);
        if (manufacturer is null)
        {
            return NotFound();
        }

        manufacturer.IsActive = !manufacturer.IsActive;
        manufacturer.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(manufacturer.IsActive ? "ManufacturerActivated" : "ManufacturerDeactivated", "Manufacturer", manufacturer.Id.ToString());

        return RedirectToAction(nameof(Index));
    }
}
