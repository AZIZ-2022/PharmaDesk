namespace PharmaDesk.Web.Models.Catalog;

public class ManufacturerListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Country { get; set; }

    public bool IsActive { get; set; }
}
