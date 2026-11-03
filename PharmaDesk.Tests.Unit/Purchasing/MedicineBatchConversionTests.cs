using PharmaDesk.Application.Purchasing;

namespace PharmaDesk.Tests.Unit.Purchasing;

public class MedicineBatchConversionTests
{
    // Exercises the same PurchaseBatchCalculator the controller calls when
    // turning a purchase line (entered in pack units, matching the supplier
    // invoice) into a MedicineBatch (stored in base units for Module 7 to
    // consume). This is the one place a silent off-by-PackSize bug could hide.

    [Theory]
    [InlineData(5, 10, 1250, 50, 125)]     // 5 strips of 10 tablets, 1250/strip -> 50 tablets, 125/tablet
    [InlineData(2, 1, 100, 2, 100)]        // PackSize of 1 (pack unit == base unit): no conversion should occur
    [InlineData(3.5, 20, 400, 70, 20)]     // fractional pack quantity
    public void ConvertsPacksToExpectedBaseUnitsAndCost(
        decimal quantityPacks, decimal packSize, decimal purchasePricePerPack,
        decimal expectedQuantityReceived, decimal expectedCostPerBaseUnit)
    {
        var quantityReceived = PurchaseBatchCalculator.ConvertPacksToBaseUnits(quantityPacks, packSize);
        var costPerBaseUnit = PurchaseBatchCalculator.CalculateCostPerBaseUnit(purchasePricePerPack, packSize);

        Assert.Equal(expectedQuantityReceived, quantityReceived);
        Assert.Equal(expectedCostPerBaseUnit, costPerBaseUnit);
    }
}
