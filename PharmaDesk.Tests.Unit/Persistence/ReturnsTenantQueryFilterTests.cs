using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Returns;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class ReturnsTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfReturnsAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.SalesReturns.AddRange(
            new SalesReturn { Id = 1, TenantId = 1, BranchId = 1, SaleId = 1, SaleItemId = 1, CustomerId = 1, Reason = "Other" },
            new SalesReturn { Id = 2, TenantId = 2, BranchId = 2, SaleId = 2, SaleItemId = 2, CustomerId = 2, Reason = "Other" });

        context.PurchaseReturns.AddRange(
            new PurchaseReturn { Id = 1, TenantId = 1, BranchId = 1, PurchaseId = 1, MedicineBatchId = 1, SupplierId = 1, Reason = "Other" },
            new PurchaseReturn { Id = 2, TenantId = 2, BranchId = 2, PurchaseId = 2, MedicineBatchId = 2, SupplierId = 2, Reason = "Other" });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsReturnsAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfReturnsAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        Assert.Single(await context.SalesReturns.ToListAsync());
        Assert.Single(await context.PurchaseReturns.ToListAsync());
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllReturnsAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfReturnsAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.SalesReturns.CountAsync());
        Assert.Equal(2, await context.PurchaseReturns.CountAsync());
    }
}
