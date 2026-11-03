using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Tenant;

public class TenantEditViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Pharmacy name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Trade license number")]
    public string? LicenseNumber { get; set; }

    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Address { get; set; }
}
