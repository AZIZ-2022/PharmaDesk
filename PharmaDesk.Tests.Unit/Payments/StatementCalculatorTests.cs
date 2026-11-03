using PharmaDesk.Application.Payments;

namespace PharmaDesk.Tests.Unit.Payments;

public class StatementCalculatorTests
{
    [Fact]
    public void NoEntries_ReturnsEmptyList()
    {
        var balances = StatementCalculator.ComputeRunningBalances(100, Array.Empty<decimal>());

        Assert.Empty(balances);
    }

    [Fact]
    public void SingleDebitEntry_AddsToOpeningBalance()
    {
        var balances = StatementCalculator.ComputeRunningBalances(100, new[] { 50m });

        Assert.Equal(new[] { 150m }, balances);
    }

    [Fact]
    public void SingleCreditEntry_SubtractsFromOpeningBalance()
    {
        var balances = StatementCalculator.ComputeRunningBalances(100, new[] { -40m });

        Assert.Equal(new[] { 60m }, balances);
    }

    [Fact]
    public void MixedEntries_AccumulateInOrder()
    {
        // Opening 0; sale due +500; payment -200; return -50; sale due +100.
        var balances = StatementCalculator.ComputeRunningBalances(0, new[] { 500m, -200m, -50m, 100m });

        Assert.Equal(new[] { 500m, 300m, 250m, 350m }, balances);
    }

    [Fact]
    public void BalanceCanGoNegative_WhenCreditsExceedDebits()
    {
        var balances = StatementCalculator.ComputeRunningBalances(50, new[] { -100m });

        Assert.Equal(new[] { -50m }, balances);
    }
}
