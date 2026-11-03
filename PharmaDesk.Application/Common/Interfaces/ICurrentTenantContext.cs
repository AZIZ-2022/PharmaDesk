namespace PharmaDesk.Application.Common.Interfaces;

public interface ICurrentTenantContext
{
    long? TenantId { get; }

    long? BranchId { get; }
}
