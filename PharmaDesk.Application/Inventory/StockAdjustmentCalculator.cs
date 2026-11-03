namespace PharmaDesk.Application.Inventory;

public static class StockAdjustmentCalculator
{
    public static bool TryApplyAdjustment(decimal currentQuantity, decimal delta, out decimal newQuantity)
    {
        newQuantity = currentQuantity + delta;
        return newQuantity >= 0;
    }
}
