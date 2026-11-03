namespace PharmaDesk.Application.Returns;

public record OriginalBatchAllocationSnapshot(long BatchId, decimal QuantityAllocated);

public record BatchCreditResult(long BatchId, decimal QuantityCredited);

public static class SalesReturnAllocator
{
    /// <summary>
    /// Credits a returned quantity back into the exact batches the original sale line
    /// drew from (not re-derived FEFO), walking the original allocations in their
    /// original order. Returns false (with an empty credit list) if the original
    /// allocations don't cover the full return quantity - callers must treat this as
    /// "reject the whole return," never apply a partial credit.
    /// </summary>
    public static bool TryAllocateReturn(IReadOnlyList<OriginalBatchAllocationSnapshot> originalAllocationsInOrder, decimal quantityToReturn, out List<BatchCreditResult> credits)
    {
        credits = new List<BatchCreditResult>();
        var remaining = quantityToReturn;

        foreach (var allocation in originalAllocationsInOrder)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (allocation.QuantityAllocated <= 0)
            {
                continue;
            }

            var credit = Math.Min(remaining, allocation.QuantityAllocated);
            credits.Add(new BatchCreditResult(allocation.BatchId, credit));
            remaining -= credit;
        }

        if (remaining > 0)
        {
            credits.Clear();
            return false;
        }

        return true;
    }
}
