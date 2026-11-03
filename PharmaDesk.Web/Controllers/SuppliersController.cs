using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Partners;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public SuppliersController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Suppliers.View)]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _dbContext.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(s => s.Name.Contains(q) || (s.Phone != null && s.Phone.Contains(q)));
        }

        var suppliers = await query
            .OrderBy(s => s.Name)
            .Select(s => new SupplierListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                CurrentBalance = s.CurrentBalance,
                IsActive = s.IsActive,
            })
            .ToListAsync();

        ViewData["SearchTerm"] = q;

        return View(suppliers);
    }

    [PermissionAuthorize(PermissionCodes.Suppliers.Manage)]
    public IActionResult Create()
    {
        return View(new SupplierFormViewModel());
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Suppliers.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var supplier = new Supplier
        {
            TenantId = tenantId,
            Name = model.Name,
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            OpeningBalance = model.OpeningBalance,
            CurrentBalance = model.OpeningBalance,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("SupplierCreated", "Supplier", supplier.Id.ToString(), $"Created supplier {supplier.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Suppliers.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        var model = new SupplierFormViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            OpeningBalance = supplier.OpeningBalance,
            IsEditMode = true,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Suppliers.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, SupplierFormViewModel model)
    {
        model.IsEditMode = true;

        var supplier = await _dbContext.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // OpeningBalance can be corrected while editing, but CurrentBalance is
        // live transactional state (Module 5/9 onward) and must never be reset
        // here - only adjust it by the same delta as the opening balance edit.
        var openingBalanceDelta = model.OpeningBalance - supplier.OpeningBalance;

        supplier.Name = model.Name;
        supplier.ContactPerson = model.ContactPerson;
        supplier.Phone = model.Phone;
        supplier.Email = model.Email;
        supplier.Address = model.Address;
        supplier.OpeningBalance = model.OpeningBalance;
        supplier.CurrentBalance += openingBalanceDelta;
        supplier.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("SupplierUpdated", "Supplier", supplier.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Suppliers.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.IsActive = !supplier.IsActive;
        supplier.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(supplier.IsActive ? "SupplierActivated" : "SupplierDeactivated", "Supplier", supplier.Id.ToString());

        return RedirectToAction(nameof(Index));
    }
}
