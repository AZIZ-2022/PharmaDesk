using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Finance;

public class CashDrawerOpenFormViewModel
{
    [Required]
    [Display(Name = "Branch")]
    public long BranchId { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Opening cash amount")]
    public decimal OpeningCashAmount { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();
}
