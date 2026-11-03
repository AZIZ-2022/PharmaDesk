using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaDesk.Web.Models.Purchasing;

public class PurchaseFormViewModel
{
    [Required]
    [Display(Name = "Supplier")]
    public long SupplierId { get; set; }

    [Required]
    [Display(Name = "Branch")]
    public long BranchId { get; set; }

    [Required]
    [Display(Name = "Purchase date")]
    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Invoice / challan number")]
    public string? InvoiceNumber { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Tax / VAT")]
    public decimal TaxAmount { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Amount paid now")]
    public decimal AmountPaid { get; set; }

    public List<PurchaseItemFormViewModel> Items { get; set; } = new();

    public List<SelectListItem> AvailableSuppliers { get; set; } = new();

    public List<SelectListItem> AvailableBranches { get; set; } = new();

    public List<MedicineOptionViewModel> AvailableMedicines { get; set; } = new();
}

public class MedicineOptionViewModel
{
    public long Id { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string PackUnitName { get; set; } = string.Empty;

    public decimal PackSize { get; set; }

    public string BaseUnitName { get; set; } = string.Empty;
}
