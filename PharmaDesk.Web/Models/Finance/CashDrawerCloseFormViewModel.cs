using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Finance;

public class CashDrawerCloseFormViewModel
{
    public long SessionId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public DateOnly SessionDate { get; set; }

    public decimal OpeningCashAmount { get; set; }

    public decimal ExpectedClosingCashAmount { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Actual counted cash")]
    public decimal ClosingCashAmount { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
