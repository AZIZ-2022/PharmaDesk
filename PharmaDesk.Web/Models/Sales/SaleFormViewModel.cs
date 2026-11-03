using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Sales;

public class SaleFormViewModel
{
    [Required]
    [Display(Name = "Branch")]
    public long BranchId { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public long CustomerId { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Tax / VAT")]
    public decimal TaxAmount { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Amount paid")]
    public decimal AmountPaid { get; set; }

    [Display(Name = "Payment method")]
    public string? PaymentMethod { get; set; }

    public List<SaleItemFormViewModel> Items { get; set; } = new();

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public List<SelectListItem> AvailableCustomers { get; set; } = new();
}
