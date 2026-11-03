namespace PharmaDesk.Application.Common.Interfaces;

public record BackupFileInfo(string FileName, long SizeBytes, DateTime CreatedAtUtc);

public interface IBackupService
{
    Task<BackupFileInfo> CreateBackupAsync(CancellationToken cancellationToken = default);

    List<BackupFileInfo> ListBackups();

    bool DeleteBackup(string fileName);

    void ApplyRetention();

    string? GetFullPath(string fileName);

    string BuildRestoreScript(string fileName);
}
