using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Infrastructure.Identity;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Roles;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class RolesController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly PharmaDeskDbContext _dbContext;
    private readonly IAuditLogService _auditLog;

    public RolesController(RoleManager<ApplicationRole> roleManager, PharmaDeskDbContext dbContext, IAuditLogService auditLog)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Roles.View)]
    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        var model = new List<RoleListItemViewModel>();
        foreach (var role in roles)
        {
            var permissionCount = await _dbContext.RolePermissions.CountAsync(rp => rp.RoleId == role.Id);
            model.Add(new RoleListItemViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                PermissionCount = permissionCount,
            });
        }

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Roles.Manage)]
    public async Task<IActionResult> Create()
    {
        var model = new RoleEditViewModel { Permissions = await BuildPermissionCheckboxesAsync(null) };
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Roles.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Permissions = await BuildPermissionCheckboxesAsync(null, model.Permissions);
            return View(model);
        }

        var role = new ApplicationRole(model.Name) { Description = model.Description };
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Permissions = await BuildPermissionCheckboxesAsync(null, model.Permissions);
            return View(model);
        }

        await SaveGrantedPermissionsAsync(role.Id, model.Permissions);
        await _auditLog.LogAsync("RoleCreated", "Role", role.Id.ToString(), $"Created role {role.Name}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Roles.Manage)]
    public async Task<IActionResult> Edit(long id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return NotFound();
        }

        var model = new RoleEditViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsEditMode = true,
            Permissions = await BuildPermissionCheckboxesAsync(role.Id),
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Roles.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, RoleEditViewModel model)
    {
        model.IsEditMode = true;

        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Permissions = await BuildPermissionCheckboxesAsync(id, model.Permissions);
            return View(model);
        }

        role.Name = model.Name;
        role.Description = model.Description;
        await _roleManager.UpdateAsync(role);

        await SaveGrantedPermissionsAsync(id, model.Permissions);
        await _auditLog.LogAsync("RolePermissionsUpdated", "Role", id.ToString());

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<PermissionCheckboxViewModel>> BuildPermissionCheckboxesAsync(
        long? roleId,
        List<PermissionCheckboxViewModel>? postedSelections = null)
    {
        var allPermissions = await _dbContext.Permissions
            .OrderBy(p => p.ModuleName)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var grantedIds = roleId.HasValue
            ? await _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId.Value).Select(rp => rp.PermissionId).ToListAsync()
            : new List<int>();

        var postedGrantedIds = postedSelections?.Where(p => p.IsGranted).Select(p => p.PermissionId).ToHashSet();

        return allPermissions.Select(p => new PermissionCheckboxViewModel
        {
            PermissionId = p.Id,
            Code = p.Code,
            Name = p.Name,
            ModuleName = p.ModuleName,
            IsGranted = postedGrantedIds?.Contains(p.Id) ?? grantedIds.Contains(p.Id),
        }).ToList();
    }

    private async Task SaveGrantedPermissionsAsync(long roleId, List<PermissionCheckboxViewModel> permissions)
    {
        var existing = _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId);
        _dbContext.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in permissions.Where(p => p.IsGranted).Select(p => p.PermissionId))
        {
            _dbContext.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        await _dbContext.SaveChangesAsync();
    }
}
