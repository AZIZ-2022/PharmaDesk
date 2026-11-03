using PharmaDesk.Application.Inventory;

namespace PharmaDesk.Tests.Unit.Inventory;

public class StockAdjustmentCalculatorTests
{
    [Theory]
    [InlineData(10, -5, 5)]
    [InlineData(10, 5, 15)]
    [InlineData(10, -10, 0)]
    public void AllowsAdjustmentThatKeepsQuantityAtOrAboveZero(decimal current, decimal delta, decimal expected)
    {
        var allowed = StockAdjustmentCalculator.TryApplyAdjustment(current, delta, out var newQuantity);

        Assert.True(allowed);
        Assert.Equal(expected, newQuantity);
    }

    [Theory]
    [InlineData(10, -10.01)]
    [InlineData(0, -0.01)]
    public void RejectsAdjustmentThatWouldMakeQuantityNegative(decimal current, decimal delta)
    {
        var allowed = StockAdjustmentCalculator.TryApplyAdjustment(current, delta, out _);

        Assert.False(allowed);
    }
}
