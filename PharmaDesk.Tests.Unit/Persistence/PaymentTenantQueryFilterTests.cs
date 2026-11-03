using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Payments;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class PaymentTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfPaymentsAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.CustomerPayments.AddRange(
            new CustomerPayment { Id = 1, TenantId = 1, BranchId = 1, CustomerId = 1, Amount = 10, PaymentDate = DateTime.UtcNow },
            new CustomerPayment { Id = 2, TenantId = 2, BranchId = 2, CustomerId = 2, Amount = 20, PaymentDate = DateTime.UtcNow });

        context.SupplierPayments.AddRange(
            new SupplierPayment { Id = 1, TenantId = 1, BranchId = 1, SupplierId = 1, Amount = 30, PaymentDate = DateTime.UtcNow },
            new SupplierPayment { Id = 2, TenantId = 2, BranchId = 2, SupplierId = 2, Amount = 40, PaymentDate = DateTime.UtcNow });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsPaymentsAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPaymentsAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        Assert.Single(await context.CustomerPayments.ToListAsync());
        Assert.Single(await context.SupplierPayments.ToListAsync());
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllPaymentsAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfPaymentsAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.CustomerPayments.CountAsync());
        Assert.Equal(2, await context.SupplierPayments.CountAsync());
    }
}
