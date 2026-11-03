using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Infrastructure.Services;

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(PharmaDeskDbContext dbContext, ILogger<DatabaseHealthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var latestVersion = await _dbContext.DatabaseSchemaVersions
                .OrderByDescending(v => v.AppliedAtUtc)
                .Select(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            return new DatabaseHealthResult
            {
                IsConnected = true,
                CurrentSchemaVersion = latestVersion,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed — PharmaDesk database may not be reachable yet.");

            return new DatabaseHealthResult
            {
                IsConnected = false,
                ErrorMessage = ex.Message,
            };
        }
    }
}
