using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Organization;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Branches;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class BranchesController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public BranchesController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Branches.View)]
    public async Task<IActionResult> Index()
    {
        var branches = await _dbContext.Branches
            .OrderByDescending(b => b.IsMainBranch)
            .ThenBy(b => b.Name)
            .Select(b => new BranchListItemViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address,
                Phone = b.Phone,
                IsMainBranch = b.IsMainBranch,
                IsActive = b.IsActive,
            })
            .ToListAsync();

        return View(branches);
    }

    [PermissionAuthorize(PermissionCodes.Branches.Manage)]
    public IActionResult Create()
    {
        return View(new BranchFormViewModel());
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Branches.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var branch = new Branch
        {
            TenantId = tenantId,
            Name = model.Name,
            Address = model.Address,
            Phone = model.Phone,
            IsMainBranch = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Branches.Add(branch);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("BranchCreated", "Branch", branch.Id.ToString(), $"Created branch {branch.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Branches.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        var model = new BranchFormViewModel
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Phone = branch.Phone,
            IsEditMode = true,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Branches.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, BranchFormViewModel model)
    {
        model.IsEditMode = true;

        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        branch.Name = model.Name;
        branch.Address = model.Address;
        branch.Phone = model.Phone;
        branch.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("BranchUpdated", "Branch", branch.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Branches.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        if (branch.IsActive)
        {
            var otherActiveBranches = await _dbContext.Branches.CountAsync(b => b.Id != id && b.IsActive);
            if (otherActiveBranches == 0)
            {
                TempData["StatusMessage"] = "Cannot deactivate the last active branch.";
                return RedirectToAction(nameof(Index));
            }
        }

        branch.IsActive = !branch.IsActive;
        branch.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(branch.IsActive ? "BranchActivated" : "BranchDeactivated", "Branch", branch.Id.ToString());

        return RedirectToAction(nameof(Index));
    }
}
