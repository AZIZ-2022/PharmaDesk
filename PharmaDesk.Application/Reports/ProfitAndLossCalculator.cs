namespace PharmaDesk.Application.Reports;

public record ProfitAndLossResult(decimal Revenue, decimal Cogs, decimal GrossProfit, decimal Expenses, decimal NetProfit);

public static class ProfitAndLossCalculator
{
    /// <summary>
    /// GrossProfit = Revenue - COGS (COGS from actual batch cost, not current purchase
    /// price - see SaleBatchAllocations.CostPerBaseUnitAtSale). NetProfit = GrossProfit
    /// - Expenses.
    /// </summary>
    public static ProfitAndLossResult Compute(decimal revenue, decimal cogs, decimal expenses)
    {
        var grossProfit = revenue - cogs;
        var netProfit = grossProfit - expenses;

        return new ProfitAndLossResult(revenue, cogs, grossProfit, expenses, netProfit);
    }
}
