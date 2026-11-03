namespace PharmaDesk.Infrastructure.Services;

public class BackupSettings
{
    public string DirectoryPath { get; set; } = string.Empty;

    public int RetentionCount { get; set; } = 14;

    public int DailyBackupHourUtc { get; set; } = 2;
}
