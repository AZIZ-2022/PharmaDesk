namespace PharmaDesk.Web.Models.Partners;

public class SupplierListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; }
}
