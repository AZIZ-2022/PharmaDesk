namespace PharmaDesk.Application.Purchasing;

public static class PurchaseBatchCalculator
{
    public static decimal ConvertPacksToBaseUnits(decimal quantityPacks, decimal packSize) => quantityPacks * packSize;

    public static decimal CalculateCostPerBaseUnit(decimal purchasePricePerPack, decimal packSize) => purchasePricePerPack / packSize;
}
