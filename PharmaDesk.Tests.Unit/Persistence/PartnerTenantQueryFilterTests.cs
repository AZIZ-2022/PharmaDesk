using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class PartnerTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfPartnersAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.Suppliers.AddRange(
            new Supplier { Id = 1, TenantId = 1, Name = "Tenant 1 Supplier" },
            new Supplier { Id = 2, TenantId = 2, Name = "Tenant 2 Supplier" });

        context.Customers.AddRange(
            new Customer { Id = 1, TenantId = 1, Name = "Walk-in Customer", IsWalkInDefault = true },
            new Customer { Id = 2, TenantId = 2, Name = "Walk-in Customer", IsWalkInDefault = true });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsSuppliersAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPartnersAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        var suppliers = await context.Suppliers.ToListAsync();

        Assert.Single(suppliers);
        Assert.Equal(1, suppliers[0].TenantId);
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsCustomersAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPartnersAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 2 }, dbName);

        var customers = await context.Customers.ToListAsync();

        Assert.Single(customers);
        Assert.Equal(2, customers[0].TenantId);
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllPartnersAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPartnersAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.Suppliers.CountAsync());
        Assert.Equal(2, await context.Customers.CountAsync());
    }
}
