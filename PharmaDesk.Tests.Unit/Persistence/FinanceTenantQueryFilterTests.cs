using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Finance;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class FinanceTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfFinanceDataAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        context.ExpenseCategories.AddRange(
            new ExpenseCategory { Id = 1, TenantId = 1, Name = "Rent" },
            new ExpenseCategory { Id = 2, TenantId = 2, Name = "Rent" });

        context.Expenses.AddRange(
            new Expense { Id = 1, TenantId = 1, BranchId = 1, ExpenseCategoryId = 1, Amount = 100, ExpenseDate = DateTime.UtcNow },
            new Expense { Id = 2, TenantId = 2, BranchId = 2, ExpenseCategoryId = 2, Amount = 200, ExpenseDate = DateTime.UtcNow });

        context.CashDrawerSessions.AddRange(
            new CashDrawerSession { Id = 1, TenantId = 1, BranchId = 1, SessionDate = DateOnly.FromDateTime(DateTime.Today), OpeningCashAmount = 500 },
            new CashDrawerSession { Id = 2, TenantId = 2, BranchId = 2, SessionDate = DateOnly.FromDateTime(DateTime.Today), OpeningCashAmount = 600 });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsFinanceDataIsVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfFinanceDataAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        Assert.Single(await context.ExpenseCategories.ToListAsync());
        Assert.Single(await context.Expenses.ToListAsync());
        Assert.Single(await context.CashDrawerSessions.ToListAsync());
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllFinanceDataIsVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfFinanceDataAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        Assert.Equal(2, await context.ExpenseCategories.CountAsync());
        Assert.Equal(2, await context.Expenses.CountAsync());
        Assert.Equal(2, await context.CashDrawerSessions.CountAsync());
    }
}
