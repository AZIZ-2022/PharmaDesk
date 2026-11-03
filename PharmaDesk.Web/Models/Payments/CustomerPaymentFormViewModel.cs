using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Payments;

public class CustomerPaymentFormViewModel
{
    public long CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal CurrentBalance { get; set; }

    [Range(0.01, 100000000)]
    [Display(Name = "Amount received")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment method")]
    public string? PaymentMethod { get; set; }

    [Required]
    [Display(Name = "Payment date")]
    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
