using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class StockAdjustmentTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfAdjustmentsAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.StockAdjustments.AddRange(
            new StockAdjustment { Id = 1, TenantId = 1, BranchId = 1, MedicineBatchId = 1, QuantityDelta = -5, Reason = "Damage" },
            new StockAdjustment { Id = 2, TenantId = 2, BranchId = 2, MedicineBatchId = 2, QuantityDelta = -3, Reason = "Expired" });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsAdjustmentsAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfAdjustmentsAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        var adjustments = await context.StockAdjustments.ToListAsync();

        Assert.Single(adjustments);
        Assert.Equal(1, adjustments[0].TenantId);
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllAdjustmentsAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfAdjustmentsAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.StockAdjustments.CountAsync());
    }
}
