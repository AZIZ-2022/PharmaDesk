using PharmaDesk.Application.Returns;

namespace PharmaDesk.Tests.Unit.Returns;

public class SalesReturnAllocatorTests
{
    [Fact]
    public void SingleOriginalBatch_FullReturn_CreditsItEntirely()
    {
        var original = new List<OriginalBatchAllocationSnapshot> { new(1, 50) };

        var ok = SalesReturnAllocator.TryAllocateReturn(original, 50, out var credits);

        Assert.True(ok);
        Assert.Single(credits);
        Assert.Equal(1, credits[0].BatchId);
        Assert.Equal(50, credits[0].QuantityCredited);
    }

    [Fact]
    public void SingleOriginalBatch_PartialReturn_CreditsOnlyRequestedAmount()
    {
        var original = new List<OriginalBatchAllocationSnapshot> { new(1, 50) };

        var ok = SalesReturnAllocator.TryAllocateReturn(original, 10, out var credits);

        Assert.True(ok);
        Assert.Single(credits);
        Assert.Equal(10, credits[0].QuantityCredited);
    }

    [Fact]
    public void MultipleOriginalBatches_ReturnSpanningBoth_CreditsInOriginalOrder()
    {
        // The original sale drew 12 from batch 1 (fully) and 8 from batch 2.
        // Returning 15 should credit all 12 back to batch 1, then 3 to batch 2.
        var original = new List<OriginalBatchAllocationSnapshot> { new(1, 12), new(2, 8) };

        var ok = SalesReturnAllocator.TryAllocateReturn(original, 15, out var credits);

        Assert.True(ok);
        Assert.Equal(2, credits.Count);
        Assert.Equal(1, credits[0].BatchId);
        Assert.Equal(12, credits[0].QuantityCredited);
        Assert.Equal(2, credits[1].BatchId);
        Assert.Equal(3, credits[1].QuantityCredited);
    }

    [Fact]
    public void ReturnExceedingOriginalTotal_FailsWithNoCredits()
    {
        var original = new List<OriginalBatchAllocationSnapshot> { new(1, 12), new(2, 8) };

        var ok = SalesReturnAllocator.TryAllocateReturn(original, 21, out var credits);

        Assert.False(ok);
        Assert.Empty(credits);
    }

    [Fact]
    public void FractionalQuantities_CreditCorrectly()
    {
        var original = new List<OriginalBatchAllocationSnapshot> { new(1, 10.5m) };

        var ok = SalesReturnAllocator.TryAllocateReturn(original, 3.25m, out var credits);

        Assert.True(ok);
        Assert.Equal(3.25m, credits[0].QuantityCredited);
    }

    [Fact]
    public void ZeroQuantityOriginalAllocations_AreSkipped()
    {
        var original = new List<OriginalBatchAllocationSnapshot> { new(1, 0), new(2, 10) };

        var ok = SalesReturnAllocator.TryAllocateReturn(original, 10, out var credits);

        Assert.True(ok);
        Assert.Single(credits);
        Assert.Equal(2, credits[0].BatchId);
    }
}
