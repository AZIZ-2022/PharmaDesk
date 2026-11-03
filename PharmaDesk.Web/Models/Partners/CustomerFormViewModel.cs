using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Partners;

public class CustomerFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Customer name")]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    [Display(Name = "Credit eligible")]
    public bool IsCreditEligible { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Credit limit")]
    public decimal? CreditLimit { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Opening balance (amount this customer owes)")]
    public decimal OpeningBalance { get; set; }

    public bool IsEditMode { get; set; }
}
