using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Sales;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class SaleTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfSalesAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.Sales.AddRange(
            new Sale { Id = 1, TenantId = 1, BranchId = 1, CustomerId = 1, SaleDate = DateTime.UtcNow },
            new Sale { Id = 2, TenantId = 2, BranchId = 2, CustomerId = 2, SaleDate = DateTime.UtcNow });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsSalesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfSalesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        var sales = await context.Sales.ToListAsync();

        Assert.Single(sales);
        Assert.Equal(1, sales[0].TenantId);
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllSalesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfSalesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.Sales.CountAsync());
    }
}
