using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Partners;

public class SupplierFormViewModel
{
    public long Id { get; set; }

    [Required]
    [Display(Name = "Supplier name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Contact person")]
    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Opening balance (amount owed to this supplier)")]
    public decimal OpeningBalance { get; set; }

    public bool IsEditMode { get; set; }
}
