namespace PharmaDesk.Domain.Catalog;

public class Manufacturer
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Country { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
}
