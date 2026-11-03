using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Tenant;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class TenantController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly IAuditLogService _auditLog;

    public TenantController(PharmaDeskDbContext dbContext, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Tenants.Manage)]
    public async Task<IActionResult> Index()
    {
        var tenant = await _dbContext.Tenants.OrderBy(t => t.Id).FirstOrDefaultAsync();
        if (tenant is null)
        {
            return NotFound("No pharmacy profile exists yet. Re-run Database/PharmaDesk_Complete.sql.");
        }

        var model = new TenantEditViewModel
        {
            Id = tenant.Id,
            Name = tenant.Name,
            LicenseNumber = tenant.LicenseNumber,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Address = tenant.Address,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Tenants.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TenantEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var tenant = await _dbContext.Tenants.FindAsync(model.Id);
        if (tenant is null)
        {
            return NotFound();
        }

        tenant.Name = model.Name;
        tenant.LicenseNumber = model.LicenseNumber;
        tenant.Phone = model.Phone;
        tenant.Email = model.Email;
        tenant.Address = model.Address;
        tenant.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditLog.LogAsync("TenantProfileUpdated", "Tenant", tenant.Id.ToString());

        TempData["StatusMessage"] = "Pharmacy profile updated.";
        return RedirectToAction(nameof(Index));
    }
}
