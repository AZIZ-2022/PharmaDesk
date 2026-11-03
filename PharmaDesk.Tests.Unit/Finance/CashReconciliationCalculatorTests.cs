using PharmaDesk.Application.Finance;

namespace PharmaDesk.Tests.Unit.Finance;

public class CashReconciliationCalculatorTests
{
    [Fact]
    public void ComputeExpectedClosing_AddsInflowsAndSubtractsOutflows()
    {
        var expected = CashReconciliationCalculator.ComputeExpectedClosing(
            openingCashAmount: 1000,
            cashSalesTotal: 500,
            cashCustomerPaymentsTotal: 200,
            cashExpensesTotal: 150,
            cashSupplierPaymentsTotal: 100);

        // 1000 + 500 + 200 - 150 - 100 = 1450
        Assert.Equal(1450m, expected);
    }

    [Fact]
    public void ComputeExpectedClosing_WithNoActivity_EqualsOpening()
    {
        var expected = CashReconciliationCalculator.ComputeExpectedClosing(1000, 0, 0, 0, 0);

        Assert.Equal(1000m, expected);
    }

    [Theory]
    [InlineData(1450, 1450, 0)]
    [InlineData(1500, 1450, 50)]
    [InlineData(1400, 1450, -50)]
    public void ComputeVariance_IsActualMinusExpected(decimal actual, decimal expected, decimal expectedVariance)
    {
        var variance = CashReconciliationCalculator.ComputeVariance(actual, expected);

        Assert.Equal(expectedVariance, variance);
    }
}
