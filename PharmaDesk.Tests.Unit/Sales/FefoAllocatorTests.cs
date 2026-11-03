using PharmaDesk.Application.Sales;

namespace PharmaDesk.Tests.Unit.Sales;

public class FefoAllocatorTests
{
    private static BatchStockSnapshot Batch(long id, decimal remaining, decimal cost, decimal price, int daysUntilExpiry) =>
        new(id, remaining, cost, price, DateOnly.FromDateTime(DateTime.Today).AddDays(daysUntilExpiry));

    [Fact]
    public void SingleBatch_ExactMatch_AllocatesEverythingFromThatBatch()
    {
        var batches = new List<BatchStockSnapshot> { Batch(1, 50, 10, 15, 30) };

        var ok = FefoAllocator.TryAllocate(batches, 50, out var allocations);

        Assert.True(ok);
        Assert.Single(allocations);
        Assert.Equal(1, allocations[0].BatchId);
        Assert.Equal(50, allocations[0].QuantityAllocated);
        Assert.Equal(10, allocations[0].CostPerBaseUnit);
    }

    [Fact]
    public void SingleBatch_HasMoreThanRequested_OnlyTakesWhatWasRequested()
    {
        var batches = new List<BatchStockSnapshot> { Batch(1, 100, 10, 15, 30) };

        var ok = FefoAllocator.TryAllocate(batches, 30, out var allocations);

        Assert.True(ok);
        Assert.Single(allocations);
        Assert.Equal(30, allocations[0].QuantityAllocated);
    }

    [Fact]
    public void SplitsAcrossBatches_SoonestExpiringFirst()
    {
        // 20 units needed; soonest-expiring batch only has 12, so it should
        // take all 12 from that one, then 8 from the next-soonest.
        var batches = new List<BatchStockSnapshot>
        {
            Batch(1, 12, 10, 15, 10),
            Batch(2, 100, 12, 18, 60),
        };

        var ok = FefoAllocator.TryAllocate(batches, 20, out var allocations);

        Assert.True(ok);
        Assert.Equal(2, allocations.Count);
        Assert.Equal(1, allocations[0].BatchId);
        Assert.Equal(12, allocations[0].QuantityAllocated);
        Assert.Equal(2, allocations[1].BatchId);
        Assert.Equal(8, allocations[1].QuantityAllocated);
    }

    [Fact]
    public void SplitsAcrossThreeBatches_WhenNecessary()
    {
        var batches = new List<BatchStockSnapshot>
        {
            Batch(1, 5, 10, 15, 5),
            Batch(2, 5, 11, 16, 15),
            Batch(3, 100, 12, 17, 90),
        };

        var ok = FefoAllocator.TryAllocate(batches, 12, out var allocations);

        Assert.True(ok);
        Assert.Equal(3, allocations.Count);
        Assert.Equal(5, allocations[0].QuantityAllocated);
        Assert.Equal(5, allocations[1].QuantityAllocated);
        Assert.Equal(2, allocations[2].QuantityAllocated);
    }

    [Fact]
    public void InsufficientTotalStock_ReturnsFalseAndNoAllocations()
    {
        var batches = new List<BatchStockSnapshot>
        {
            Batch(1, 5, 10, 15, 10),
            Batch(2, 3, 11, 16, 20),
        };

        var ok = FefoAllocator.TryAllocate(batches, 10, out var allocations);

        Assert.False(ok);
        Assert.Empty(allocations);
    }

    [Fact]
    public void ZeroQuantityBatchesInTheMiddle_AreSkipped()
    {
        var batches = new List<BatchStockSnapshot>
        {
            Batch(1, 5, 10, 15, 10),
            Batch(2, 0, 11, 16, 20),
            Batch(3, 10, 12, 17, 30),
        };

        var ok = FefoAllocator.TryAllocate(batches, 10, out var allocations);

        Assert.True(ok);
        Assert.Equal(2, allocations.Count);
        Assert.DoesNotContain(allocations, a => a.BatchId == 2);
    }

    [Fact]
    public void FractionalQuantities_AllocateCorrectly()
    {
        var batches = new List<BatchStockSnapshot> { Batch(1, 10.5m, 10, 15, 30) };

        var ok = FefoAllocator.TryAllocate(batches, 3.25m, out var allocations);

        Assert.True(ok);
        Assert.Equal(3.25m, allocations[0].QuantityAllocated);
    }

    [Fact]
    public void NoBatches_RequestingZero_SucceedsWithNoAllocations()
    {
        var ok = FefoAllocator.TryAllocate(new List<BatchStockSnapshot>(), 0, out var allocations);

        Assert.True(ok);
        Assert.Empty(allocations);
    }

    [Fact]
    public void GetQuotedUnitPrice_ReturnsSoonestExpiringNonExhaustedBatchPrice()
    {
        var batches = new List<BatchStockSnapshot>
        {
            Batch(1, 0, 10, 15, 5),
            Batch(2, 5, 11, 16, 20),
        };

        var price = FefoAllocator.GetQuotedUnitPrice(batches);

        Assert.Equal(16, price);
    }

    [Fact]
    public void GetQuotedUnitPrice_ReturnsNull_WhenNoStockAtAll()
    {
        var batches = new List<BatchStockSnapshot> { Batch(1, 0, 10, 15, 5) };

        var price = FefoAllocator.GetQuotedUnitPrice(batches);

        Assert.Null(price);
    }
}
