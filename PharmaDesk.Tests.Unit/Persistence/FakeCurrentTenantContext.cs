using PharmaDesk.Application.Common.Interfaces;

namespace PharmaDesk.Tests.Unit.Persistence;

public class FakeCurrentTenantContext : ICurrentTenantContext
{
    public long? TenantId { get; set; }

    public long? BranchId { get; set; }
}
