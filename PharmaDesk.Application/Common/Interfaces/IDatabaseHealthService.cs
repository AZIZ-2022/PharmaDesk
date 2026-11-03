namespace PharmaDesk.Application.Common.Interfaces;

public interface IDatabaseHealthService
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}

public class DatabaseHealthResult
{
    public bool IsConnected { get; init; }

    public string? CurrentSchemaVersion { get; init; }

    public string? ErrorMessage { get; init; }
}
