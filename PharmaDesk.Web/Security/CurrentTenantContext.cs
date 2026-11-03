using PharmaDesk.Application.Common.Interfaces;

namespace PharmaDesk.Web.Security;

public class CurrentTenantContext : ICurrentTenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long? TenantId => ReadClaim("TenantId");

    public long? BranchId => ReadClaim("BranchId");

    private long? ReadClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        return long.TryParse(value, out var id) ? id : null;
    }
}
