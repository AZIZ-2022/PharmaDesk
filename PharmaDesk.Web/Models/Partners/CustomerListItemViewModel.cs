namespace PharmaDesk.Web.Models.Partners;

public class CustomerListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsCreditEligible { get; set; }

    public decimal CurrentBalance { get; set; }

    public bool IsWalkInDefault { get; set; }

    public bool IsActive { get; set; }
}
