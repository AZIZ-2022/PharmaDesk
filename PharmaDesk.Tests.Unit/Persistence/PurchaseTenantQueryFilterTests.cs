using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class PurchaseTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfPurchasesAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.Purchases.AddRange(
            new Purchase { Id = 1, TenantId = 1, BranchId = 1, SupplierId = 1, PurchaseDate = DateTime.UtcNow },
            new Purchase { Id = 2, TenantId = 2, BranchId = 2, SupplierId = 2, PurchaseDate = DateTime.UtcNow });

        context.MedicineBatches.AddRange(
            new MedicineBatch { Id = 1, TenantId = 1, BranchId = 1, MedicineId = 1, SupplierId = 1, PurchaseId = 1, PurchaseItemId = 1, BatchNumber = "A1", ExpiryDate = DateOnly.FromDateTime(DateTime.Today) },
            new MedicineBatch { Id = 2, TenantId = 2, BranchId = 2, MedicineId = 1, SupplierId = 2, PurchaseId = 2, PurchaseItemId = 2, BatchNumber = "A2", ExpiryDate = DateOnly.FromDateTime(DateTime.Today) });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsPurchasesAndBatchesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPurchasesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        Assert.Single(await context.Purchases.ToListAsync());
        Assert.Single(await context.MedicineBatches.ToListAsync());
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllPurchasesAndBatchesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPurchasesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.Purchases.CountAsync());
        Assert.Equal(2, await context.MedicineBatches.CountAsync());
    }
}
