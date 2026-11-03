using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PharmaDesk.Application.Common.Interfaces;

namespace PharmaDesk.Infrastructure.Services;

/// <summary>
/// Runs once a day, creates a backup, and applies retention. Checks hourly
/// whether today's scheduled hour has passed and no backup has been made
/// today yet, rather than sleeping for exactly 24 hours, so a restart doesn't
/// permanently shift the schedule.
/// </summary>
public class BackupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackupSettings _settings;
    private readonly ILogger<BackupBackgroundService> _logger;
    private DateOnly? _lastBackupDate;

    public BackupBackgroundService(IServiceScopeFactory scopeFactory, IOptions<BackupSettings> settings, ILogger<BackupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.DirectoryPath))
        {
            _logger.LogWarning("Scheduled backups are disabled: BackupSettings:DirectoryPath is not configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now);

                if (_lastBackupDate != today && now.Hour >= _settings.DailyBackupHourUtc)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

                    await backupService.CreateBackupAsync(stoppingToken);
                    backupService.ApplyRetention();

                    _lastBackupDate = today;
                    _logger.LogInformation("Scheduled daily backup completed.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scheduled daily backup failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
