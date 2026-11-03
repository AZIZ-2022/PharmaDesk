using Microsoft.EntityFrameworkCore;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Tests.Unit.Persistence;

public class MedicineTenantQueryFilterTests
{
    private static PharmaDeskDbContext CreateContext(FakeCurrentTenantContext tenantContext, string databaseName)
    {
        var options = new DbContextOptionsBuilder<PharmaDeskDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new PharmaDeskDbContext(options, tenantContext);
    }

    private static async Task SeedTwoTenantsWorthOfMedicinesAsync(string databaseName)
    {
        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, databaseName);

        // Seed the shared reference data each Medicine's FKs depend on
        // (DosageForm, UnitOfMeasure are global; Category/Manufacturer are
        // tenant-scoped but the filter is irrelevant to FK validity here).
        context.DosageForms.Add(new DosageForm { Id = 1, Name = "Tablet" });
        context.UnitsOfMeasure.AddRange(
            new UnitOfMeasure { Id = 1, Name = "Tablet", ShortCode = "tab" },
            new UnitOfMeasure { Id = 2, Name = "Strip", ShortCode = "strip" });
        context.MedicineCategories.Add(new MedicineCategory { Id = 1, TenantId = 1, Name = "Analgesic" });
        context.MedicineCategories.Add(new MedicineCategory { Id = 2, TenantId = 2, Name = "Analgesic" });
        context.Manufacturers.Add(new Manufacturer { Id = 1, TenantId = 1, Name = "Beximco" });
        context.Manufacturers.Add(new Manufacturer { Id = 2, TenantId = 2, Name = "Square" });

        context.Medicines.AddRange(
            new Medicine
            {
                Id = 1,
                TenantId = 1,
                BrandName = "Napa",
                GenericName = "Paracetamol",
                ManufacturerId = 1,
                CategoryId = 1,
                DosageFormId = 1,
                BaseUnitId = 1,
                PackUnitId = 2,
                PackSize = 10m,
            },
            new Medicine
            {
                Id = 2,
                TenantId = 2,
                BrandName = "Ace",
                GenericName = "Paracetamol",
                ManufacturerId = 2,
                CategoryId = 2,
                DosageFormId = 1,
                BaseUnitId = 1,
                PackUnitId = 2,
                PackSize = 10m,
            });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task WithAmbientTenant_OnlyThatTenantsMedicinesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfMedicinesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        var medicines = await context.Medicines.ToListAsync();

        Assert.Single(medicines);
        Assert.Equal("Napa", medicines[0].BrandName);
    }

    [Fact]
    public async Task WithNoAmbientTenant_AllMedicinesAreVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfMedicinesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = null }, dbName);

        var medicines = await context.Medicines.ToListAsync();

        Assert.Equal(2, medicines.Count);
    }

    [Fact]
    public async Task MedicineWithDistinctBaseAndPackUnits_PersistsBothForeignKeysCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoTenantsWorthOfMedicinesAsync(dbName);

        using var context = CreateContext(new FakeCurrentTenantContext { TenantId = 1 }, dbName);

        var medicine = await context.Medicines.SingleAsync();

        Assert.Equal(1, medicine.BaseUnitId);
        Assert.Equal(2, medicine.PackUnitId);
        Assert.Equal(10m, medicine.PackSize);
    }
}
