using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Finance;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Finance;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class ExpensesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public ExpensesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Expenses.View)]
    public async Task<IActionResult> Index(DateOnly? fromDate, DateOnly? toDate, long? categoryId)
    {
        var query = _dbContext.Expenses.AsQueryable();

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.ExpenseDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(e => e.ExpenseDate <= to);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);
        }

        var expenses = await query
            .Join(_dbContext.ExpenseCategories, e => e.ExpenseCategoryId, c => c.Id, (e, c) => new { e, c })
            .Join(_dbContext.Branches, x => x.e.BranchId, b => b.Id, (x, b) => new ExpenseListItemViewModel
            {
                Id = x.e.Id,
                CategoryName = x.c.Name,
                BranchName = b.Name,
                Amount = x.e.Amount,
                PaymentMethod = x.e.PaymentMethod,
                ExpenseDate = x.e.ExpenseDate,
                IsActive = x.e.IsActive,
            })
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        ViewData["AvailableCategories"] = await GetCategoryOptionsAsync();
        ViewData["FromDate"] = fromDate;
        ViewData["ToDate"] = toDate;
        ViewData["SelectedCategoryId"] = categoryId;

        return View(expenses);
    }

    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    public async Task<IActionResult> Create()
    {
        var model = new ExpenseFormViewModel();
        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseFormViewModel model)
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

        var expense = new Expense
        {
            TenantId = tenantId,
            BranchId = model.BranchId,
            ExpenseCategoryId = model.ExpenseCategoryId,
            Amount = model.Amount,
            PaymentMethod = model.PaymentMethod,
            ExpenseDate = model.ExpenseDate.ToDateTime(TimeOnly.MinValue),
            Notes = model.Notes,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("ExpenseCreated", "Expense", expense.Id.ToString(), $"Recorded expense of {expense.Amount:N2}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var expense = await _dbContext.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        var model = new ExpenseFormViewModel
        {
            Id = expense.Id,
            BranchId = expense.BranchId,
            ExpenseCategoryId = expense.ExpenseCategoryId,
            Amount = expense.Amount,
            PaymentMethod = expense.PaymentMethod,
            ExpenseDate = DateOnly.FromDateTime(expense.ExpenseDate),
            Notes = expense.Notes,
            IsEditMode = true,
        };

        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ExpenseFormViewModel model)
    {
        model.IsEditMode = true;

        var expense = await _dbContext.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        expense.BranchId = model.BranchId;
        expense.ExpenseCategoryId = model.ExpenseCategoryId;
        expense.Amount = model.Amount;
        expense.PaymentMethod = model.PaymentMethod;
        expense.ExpenseDate = model.ExpenseDate.ToDateTime(TimeOnly.MinValue);
        expense.Notes = model.Notes;
        expense.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("ExpenseUpdated", "Expense", expense.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Expenses.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var expense = await _dbContext.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        expense.IsActive = !expense.IsActive;
        expense.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(expense.IsActive ? "ExpenseRestored" : "ExpenseVoided", "Expense", expense.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCategoryOptionsAsync()
    {
        return await _dbContext.ExpenseCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }

    private async Task PopulateDropdownsAsync(ExpenseFormViewModel model)
    {
        model.AvailableBranches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();

        model.AvailableCategories = await _dbContext.ExpenseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }
}
