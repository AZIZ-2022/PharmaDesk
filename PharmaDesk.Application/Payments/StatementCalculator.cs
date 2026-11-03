namespace PharmaDesk.Application.Payments;

public static class StatementCalculator
{
    /// <summary>
    /// Computes the running balance after each entry, given entries already sorted
    /// chronologically. A positive amount increases the balance owed (e.g. a sale's
    /// due portion); a negative amount decreases it (e.g. a return or payment).
    /// </summary>
    public static IReadOnlyList<decimal> ComputeRunningBalances(decimal openingBalance, IReadOnlyList<decimal> amountsInChronologicalOrder)
    {
        var balances = new List<decimal>(amountsInChronologicalOrder.Count);
        var running = openingBalance;

        foreach (var amount in amountsInChronologicalOrder)
        {
            running += amount;
            balances.Add(running);
        }

        return balances;
    }
}
