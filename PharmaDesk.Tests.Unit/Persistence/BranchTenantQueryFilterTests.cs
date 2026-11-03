using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Organization;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class BranchTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfBranchesAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.Branches.AddRange(
            new Branch { Id = 1, TenantId = 1, Name = "Tenant 1 Branch" },
            new Branch { Id = 2, TenantId = 2, Name = "Tenant 2 Branch" });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsBranchesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfBranchesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        var branches = await context.Branches.ToListAsync();

        Assert.Single(branches);
        Assert.Equal(1, branches[0].TenantId);
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllBranchesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfBranchesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        var branches = await context.Branches.ToListAsync();

        Assert.Equal(2, branches.Count);
    }
}
