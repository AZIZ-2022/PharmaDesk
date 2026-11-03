namespace PharmaDesk.Domain.Catalog;

public class UnitOfMeasure
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
