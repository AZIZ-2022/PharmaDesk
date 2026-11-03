using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Infrastructure.Identity;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Users;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        PharmaDeskDbContext dbContext,
        ICurrentTenantContext currentTenant,
        IAuditLogService auditLog)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Users.View)]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();

        var model = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = roles,
            });
        }

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Users.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new UserFormViewModel
        {
            AvailableRoles = await GetRoleOptionsAsync(),
            AvailableBranches = await GetBranchOptionsAsync(),
        };
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Users.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        model.AvailableRoles = await GetRoleOptionsAsync();
        model.AvailableBranches = await GetBranchOptionsAsync();

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "A temporary password is required for a new user.");
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            ModelState.AddModelError(string.Empty, "Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            TenantId = _currentTenant.TenantId!.Value,
            BranchId = model.SelectedBranchId,
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        if (model.SelectedRoleId.HasValue)
        {
            var role = await _roleManager.FindByIdAsync(model.SelectedRoleId.Value.ToString());
            if (role?.Name is not null)
            {
                await _userManager.AddToRoleAsync(user, role.Name);
            }
        }

        await _auditLog.LogAsync("UserCreated", "User", user.Id.ToString(), $"Created user {user.UserName}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Users.Edit)]
    public async Task<IActionResult> Edit(long id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = await _roleManager.Roles.FirstOrDefaultAsync(r => roles.Contains(r.Name!));

        var model = new UserFormViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email,
            SelectedRoleId = currentRole?.Id,
            SelectedBranchId = user.BranchId,
            IsEditMode = true,
            AvailableRoles = await GetRoleOptionsAsync(),
            AvailableBranches = await GetBranchOptionsAsync(),
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Users.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, UserFormViewModel model)
    {
        model.IsEditMode = true;
        model.AvailableRoles = await GetRoleOptionsAsync();
        model.AvailableBranches = await GetBranchOptionsAsync();

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.BranchId = model.SelectedBranchId;
        user.ModifiedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        if (model.SelectedRoleId.HasValue)
        {
            var role = await _roleManager.FindByIdAsync(model.SelectedRoleId.Value.ToString());
            if (role?.Name is not null)
            {
                await _userManager.AddToRoleAsync(user, role.Name);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (resetResult.Succeeded)
            {
                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);
            }
        }

        await _auditLog.LogAsync("UserUpdated", "User", user.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Users.Deactivate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        user.ModifiedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, user.IsActive ? null : DateTimeOffset.MaxValue);

        await _auditLog.LogAsync(user.IsActive ? "UserActivated" : "UserDeactivated", "User", user.Id.ToString());

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetRoleOptionsAsync()
    {
        return await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetBranchOptionsAsync()
    {
        return await _dbContext.Branches
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
            .ToListAsync();
    }
}
