using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Finance;

public class ExpenseFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Branch")]
    public long BranchId { get; set; }

    [Required]
    [Display(Name = "Category")]
    public long ExpenseCategoryId { get; set; }

    [Range(0.01, 100000000)]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment method")]
    public string? PaymentMethod { get; set; }

    [Required]
    [Display(Name = "Expense date")]
    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public bool IsEditMode { get; set; }

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public List<SelectListItem> AvailableCategories { get; set; } = new();
}
