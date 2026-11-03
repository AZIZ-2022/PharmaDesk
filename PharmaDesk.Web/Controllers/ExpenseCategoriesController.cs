using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Finance;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Finance;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class ExpenseCategoriesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public ExpenseCategoriesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Expenses.View)]
    public async Task<IActionResult> Index()
    {
        var categories = await _dbContext.ExpenseCategories
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
            })
            .ToListAsync();

        return View(categories);
    }

    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    public IActionResult Create()
    {
        return View(new ExpenseCategoryFormViewModel());
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var category = new ExpenseCategory
        {
            TenantId = tenantId,
            Name = model.Name,
            Description = model.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.ExpenseCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("ExpenseCategoryCreated", "ExpenseCategory", category.Id.ToString(), $"Created expense category {category.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var category = await _dbContext.ExpenseCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var model = new ExpenseCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsEditMode = true,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ExpenseCategoryFormViewModel model)
    {
        model.IsEditMode = true;

        var category = await _dbContext.ExpenseCategories.FindAsync(id);
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
        await _auditLog.LogAsync("ExpenseCategoryUpdated", "ExpenseCategory", category.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var category = await _dbContext.ExpenseCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        category.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(category.IsActive ? "ExpenseCategoryActivated" : "ExpenseCategoryDeactivated", "ExpenseCategory", category.Id.ToString());

        return RedirectToAction(nameof(Index));
    }
}
