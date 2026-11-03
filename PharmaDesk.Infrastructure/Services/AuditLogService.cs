using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Domain.SystemEntities;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;

    public AuditLogService(PharmaDeskDbContext dbContext, ICurrentUserContext currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string? entityName = null,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = _currentUser.IpAddress,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
