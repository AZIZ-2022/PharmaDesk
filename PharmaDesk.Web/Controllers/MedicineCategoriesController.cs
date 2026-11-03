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
public class MedicineCategoriesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public MedicineCategoriesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Medicines.View)]
    public async Task<IActionResult> Index()
    {
        var categories = await _dbContext.MedicineCategories
            .OrderBy(c => c.Name)
            .Select(c => new MedicineCategoryListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
            })
            .ToListAsync();

        return View(categories);
    }

    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    public IActionResult Create()
    {
        return View(new MedicineCategoryFormViewModel());
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicineCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var category = new MedicineCategory
        {
            TenantId = tenantId,
            Name = model.Name,
            Description = model.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.MedicineCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("MedicineCategoryCreated", "MedicineCategory", category.Id.ToString(), $"Created category {category.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var category = await _dbContext.MedicineCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var model = new MedicineCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsEditMode = true,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, MedicineCategoryFormViewModel model)
    {
        model.IsEditMode = true;

        var category = await _dbContext.MedicineCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        category.Name = model.Name;
        category.Description = model.Description;
        category.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("MedicineCategoryUpdated", "MedicineCategory", category.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Medicines.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var category = await _dbContext.MedicineCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        category.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(category.IsActive ? "MedicineCategoryActivated" : "MedicineCategoryDeactivated", "MedicineCategory", category.Id.ToString());

        return RedirectToAction(nameof(Index));
    }
}
