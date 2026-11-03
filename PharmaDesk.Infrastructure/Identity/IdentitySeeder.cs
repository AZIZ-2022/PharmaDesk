using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Infrastructure.Identity;

/// <summary>
/// Seeds exactly one default admin account on a brand-new database. Roles, permissions, the
/// placeholder Tenant and its main Branch are all seeded by Database/PharmaDesk_Complete.sql
/// (plain data); only the admin user's password hash has to be produced in code, since that
/// requires Identity's own password hasher.
/// </summary>
public class IdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(UserManager<ApplicationUser> userManager, PharmaDeskDbContext dbContext, ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedDefaultAdminUserAsync()
    {
        if (await _userManager.Users.AnyAsync())
        {
            return;
        }

        var tenant = await _dbContext.Tenants.OrderBy(t => t.Id).FirstOrDefaultAsync();
        if (tenant is null)
        {
            _logger.LogError(
                "Cannot seed the default admin account: no Tenant exists yet. Run Database/PharmaDesk_Complete.sql first.");
            return;
        }

        var temporaryPassword = GenerateTemporaryPassword();

        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@localhost",
            EmailConfirmed = true,
            FullName = "System Administrator",
            TenantId = tenant.Id,
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(admin, temporaryPassword);
        if (!result.Succeeded)
        {
            _logger.LogError(
                "Failed to seed the default admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(admin, "Admin");

        _logger.LogWarning(
            "Seeded the default admin account. Username: {UserName}  Temporary password: {TemporaryPassword}  " +
            "You must change this password immediately after logging in for the first time.",
            admin.UserName,
            temporaryPassword);
    }

    internal static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var chars = new char[16];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
