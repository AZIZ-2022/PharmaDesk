namespace PharmaDesk.Domain.SystemEntities;

public class ApplicationSetting
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = string.Empty;

    public string? SettingValue { get; set; }

    public string? Description { get; set; }

    public DateTime ModifiedAtUtc { get; set; }
}
