namespace PharmaDesk.Domain.Security;

public class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
