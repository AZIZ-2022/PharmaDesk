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
public class CustomersController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public CustomersController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Customers.View)]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _dbContext.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c => c.Name.Contains(q) || (c.Phone != null && c.Phone.Contains(q)));
        }

        var customers = await query
            .OrderByDescending(c => c.IsWalkInDefault)
            .ThenBy(c => c.Name)
            .Select(c => new CustomerListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                IsCreditEligible = c.IsCreditEligible,
                CurrentBalance = c.CurrentBalance,
                IsWalkInDefault = c.IsWalkInDefault,
                IsActive = c.IsActive,
            })
            .ToListAsync();

        ViewData["SearchTerm"] = q;

        return View(customers);
    }

    [PermissionAuthorize(PermissionCodes.Customers.Manage)]
    public IActionResult Create()
    {
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Customers.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var customer = new Customer
        {
            TenantId = tenantId,
            Name = model.Name,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            IsCreditEligible = model.IsCreditEligible,
            CreditLimit = model.IsCreditEligible ? model.CreditLimit : null,
            OpeningBalance = model.OpeningBalance,
            CurrentBalance = model.OpeningBalance,
            IsWalkInDefault = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("CustomerCreated", "Customer", customer.Id.ToString(), $"Created customer {customer.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Customers.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        var model = new CustomerFormViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            IsCreditEligible = customer.IsCreditEligible,
            CreditLimit = customer.CreditLimit,
            OpeningBalance = customer.OpeningBalance,
            IsEditMode = true,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Customers.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, CustomerFormViewModel model)
    {
        model.IsEditMode = true;

        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Same delta-based correction as SuppliersController.Edit - see that
        // controller's comment for why CurrentBalance isn't simply overwritten.
        var openingBalanceDelta = model.OpeningBalance - customer.OpeningBalance;

        customer.Name = model.Name;
        customer.Phone = model.Phone;
        customer.Email = model.Email;
        customer.Address = model.Address;
        customer.IsCreditEligible = model.IsCreditEligible;
        customer.CreditLimit = model.IsCreditEligible ? model.CreditLimit : null;
        customer.OpeningBalance = model.OpeningBalance;
        customer.CurrentBalance += openingBalanceDelta;
        customer.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("CustomerUpdated", "Customer", customer.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Customers.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        if (customer.IsWalkInDefault && customer.IsActive)
        {
            TempData["StatusMessage"] = "Cannot deactivate the default Walk-in Customer — POS sales rely on it always existing.";
            return RedirectToAction(nameof(Index));
        }

        customer.IsActive = !customer.IsActive;
        customer.ModifiedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync(customer.IsActive ? "CustomerActivated" : "CustomerDeactivated", "Customer", customer.Id.ToString());

        return RedirectToAction(nameof(Index));
    }
}
