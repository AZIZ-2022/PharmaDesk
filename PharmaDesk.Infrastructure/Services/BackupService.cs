using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaDesk.Application.Backup;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly BackupSettings _settings;

    public BackupService(PharmaDeskDbContext dbContext, IOptions<BackupSettings> settings)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
    }

    public async Task<BackupFileInfo> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_settings.DirectoryPath);

        var databaseName = _dbContext.Database.GetDbConnection().Database;
        var fileName = $"{databaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(_settings.DirectoryPath, fileName);

        // BACKUP DATABASE is safe to run against a live, in-use database -
        // SQL Server supports this natively. RESTORE is the operation this
        // module deliberately does not automate - see BuildRestoreScript.
        var sql = $"BACKUP DATABASE [{databaseName}] TO DISK = @path WITH INIT, STATS = 10";
        await _dbContext.Database.ExecuteSqlRawAsync(sql, new object[] { new SqlParameter("@path", fullPath) }, cancellationToken);

        var fileInfo = new FileInfo(fullPath);
        return new BackupFileInfo(fileInfo.Name, fileInfo.Length, fileInfo.CreationTimeUtc);
    }

    public List<BackupFileInfo> ListBackups()
    {
        if (!Directory.Exists(_settings.DirectoryPath))
        {
            return new List<BackupFileInfo>();
        }

        return Directory.GetFiles(_settings.DirectoryPath, "*.bak")
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new BackupFileInfo(f.Name, f.Length, f.CreationTimeUtc))
            .ToList();
    }

    public bool DeleteBackup(string fileName)
    {
        var fullPath = GetFullPath(fileName);
        if (fullPath is null)
        {
            return false;
        }

        File.Delete(fullPath);
        return true;
    }

    public void ApplyRetention()
    {
        var backups = ListBackups();
        if (backups.Count <= _settings.RetentionCount)
        {
            return;
        }

        foreach (var old in backups.OrderByDescending(b => b.CreatedAtUtc).Skip(_settings.RetentionCount))
        {
            DeleteBackup(old.FileName);
        }
    }

    public string? GetFullPath(string fileName)
    {
        if (!BackupFileNameValidator.IsValid(fileName))
        {
            return null;
        }

        var fullPath = Path.Combine(_settings.DirectoryPath, fileName);
        return File.Exists(fullPath) ? fullPath : null;
    }

    public string BuildRestoreScript(string fileName)
    {
        var databaseName = _dbContext.Database.GetDbConnection().Database;
        var fullPath = Path.Combine(_settings.DirectoryPath, fileName);

        return
$@"-- Run this in SSMS, connected to the SQL Server instance hosting [{databaseName}].
-- Stop the PharmaDesk web application first so nothing else is using the database.
USE [master];
ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{databaseName}] FROM DISK = N'{fullPath}' WITH REPLACE;
ALTER DATABASE [{databaseName}] SET MULTI_USER;
";
    }
}
