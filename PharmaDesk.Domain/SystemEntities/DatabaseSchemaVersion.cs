namespace PharmaDesk.Domain.SystemEntities;

public class DatabaseSchemaVersion
{
    public int Id { get; set; }

    public string VersionNumber { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime AppliedAtUtc { get; set; }

    public bool Success { get; set; }
}
