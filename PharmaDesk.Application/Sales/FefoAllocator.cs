namespace PharmaDesk.Application.Sales;

public record BatchStockSnapshot(long BatchId, decimal QuantityRemaining, decimal CostPerBaseUnit, decimal SellingPricePerBaseUnit, DateOnly ExpiryDate);

public record BatchAllocationResult(long BatchId, decimal QuantityAllocated, decimal CostPerBaseUnit);

public static class FefoAllocator
{
    /// <summary>
    /// Allocates a requested quantity across batches, consuming the soonest-expiring
    /// batch first. Callers must pass <paramref name="batchesOrderedByExpiry"/> already
    /// sorted ascending by expiry date; this method does not re-sort.
    /// Returns false (with an empty allocation list) if the batches cannot cover the
    /// full requested quantity - callers must treat this as "reject the whole sale,"
    /// never apply a partial allocation.
    /// </summary>
    public static bool TryAllocate(IReadOnlyList<BatchStockSnapshot> batchesOrderedByExpiry, decimal requestedQuantity, out List<BatchAllocationResult> allocations)
    {
        allocations = new List<BatchAllocationResult>();
        var remaining = requestedQuantity;

        foreach (var batch in batchesOrderedByExpiry)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (batch.QuantityRemaining <= 0)
            {
                continue;
            }

            var take = Math.Min(remaining, batch.QuantityRemaining);
            allocations.Add(new BatchAllocationResult(batch.BatchId, take, batch.CostPerBaseUnit));
            remaining -= take;
        }

        if (remaining > 0)
        {
            allocations.Clear();
            return false;
        }

        return true;
    }

    /// <summary>
    /// The price quoted to the customer for a line: the selling price of the batch
    /// that will be consumed first (soonest-expiring with remaining stock), even if
    /// fulfilling the full quantity ends up spanning into a later batch with a
    /// different cost. Returns null if there is no stock at all.
    /// </summary>
    public static decimal? GetQuotedUnitPrice(IReadOnlyList<BatchStockSnapshot> batchesOrderedByExpiry)
    {
        return batchesOrderedByExpiry.FirstOrDefault(b => b.QuantityRemaining > 0)?.SellingPricePerBaseUnit;
    }
}
