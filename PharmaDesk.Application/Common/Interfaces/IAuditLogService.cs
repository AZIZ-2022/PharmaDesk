namespace PharmaDesk.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string? entityName = null,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}
