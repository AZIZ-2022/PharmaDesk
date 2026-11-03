using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly PharmaDeskDbContext _dbContext;

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options,
        PharmaDeskDbContext dbContext)
        : base(userManager, roleManager, options)
    {
        _dbContext = dbContext;
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        identity.AddClaim(new Claim("MustChangePassword", user.MustChangePassword ? "true" : "false"));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FullName));
        identity.AddClaim(new Claim("TenantId", user.TenantId.ToString()));

        if (user.BranchId.HasValue)
        {
            identity.AddClaim(new Claim("BranchId", user.BranchId.Value.ToString()));
        }

        var roleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var permissionCodes = await _dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_dbContext.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .Distinct()
            .ToListAsync();

        foreach (var code in permissionCodes)
        {
            identity.AddClaim(new Claim("permission", code));
        }

        return principal;
    }
}
