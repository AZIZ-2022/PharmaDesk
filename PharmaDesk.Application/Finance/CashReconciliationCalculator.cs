namespace PharmaDesk.Application.Finance;

public static class CashReconciliationCalculator
{
    /// <summary>
    /// Expected cash at close = opening + cash inflows (Sales, CustomerPayments) -
    /// cash outflows (Expenses, SupplierPayments). Deliberately excludes
    /// Sales/Purchase Returns, since those don't record a cash/non-cash settlement
    /// marker - see BusinessRules.md.
    /// </summary>
    public static decimal ComputeExpectedClosing(
        decimal openingCashAmount,
        decimal cashSalesTotal,
        decimal cashCustomerPaymentsTotal,
        decimal cashExpensesTotal,
        decimal cashSupplierPaymentsTotal)
    {
        return openingCashAmount
            + cashSalesTotal
            + cashCustomerPaymentsTotal
            - cashExpensesTotal
            - cashSupplierPaymentsTotal;
    }

    /// <summary>
    /// Positive means there's more cash in the drawer than expected; negative
    /// means less.
    /// </summary>
    public static decimal ComputeVariance(decimal actualClosingCashAmount, decimal expectedClosingCashAmount)
    {
        return actualClosingCashAmount - expectedClosingCashAmount;
    }
}
