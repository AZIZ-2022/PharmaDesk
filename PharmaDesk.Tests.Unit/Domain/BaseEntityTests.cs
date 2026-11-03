using PharmaDesk.Domain.Common;

namespace PharmaDesk.Tests.Unit.Domain;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
    }

    [Fact]
    public void NewEntity_IsNotDeletedByDefault()
    {
        var entity = new TestEntity();

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.ModifiedAtUtc);
    }
}
