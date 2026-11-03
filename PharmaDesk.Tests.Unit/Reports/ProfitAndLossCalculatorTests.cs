using PharmaDesk.Application.Reports;

namespace PharmaDesk.Tests.Unit.Reports;

public class ProfitAndLossCalculatorTests
{
    [Fact]
    public void Compute_SubtractsCogsFromRevenueForGrossProfit()
    {
        var result = ProfitAndLossCalculator.Compute(revenue: 1000, cogs: 600, expenses: 0);

        Assert.Equal(400m, result.GrossProfit);
    }

    [Fact]
    public void Compute_SubtractsExpensesFromGrossProfitForNetProfit()
    {
        var result = ProfitAndLossCalculator.Compute(revenue: 1000, cogs: 600, expenses: 150);

        Assert.Equal(400m, result.GrossProfit);
        Assert.Equal(250m, result.NetProfit);
    }

    [Fact]
    public void Compute_CanProduceANetLoss()
    {
        var result = ProfitAndLossCalculator.Compute(revenue: 500, cogs: 400, expenses: 300);

        Assert.Equal(100m, result.GrossProfit);
        Assert.Equal(-200m, result.NetProfit);
    }

    [Fact]
    public void Compute_WithNoActivity_EverythingIsZero()
    {
        var result = ProfitAndLossCalculator.Compute(0, 0, 0);

        Assert.Equal(0m, result.Revenue);
        Assert.Equal(0m, result.GrossProfit);
        Assert.Equal(0m, result.NetProfit);
    }
}
