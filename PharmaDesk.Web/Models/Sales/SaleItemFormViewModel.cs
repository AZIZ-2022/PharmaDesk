using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.Web.Models.Sales;

public class SaleItemFormViewModel
{
    [Required]
    public long MedicineId { get; set; }

    // Display only - echoed back on redisplay for the cashier's convenience.
    // Never trusted for money math; Complete always recomputes price server-side.
    public string MedicineBrandName { get; set; } = string.Empty;

    public decimal UnitPriceQuoted { get; set; }

    [Range(0.01, 1000000)]
    public decimal QuantityBaseUnits { get; set; }
}
