namespace PharmaDesk.Domain.Catalog;

public class Medicine
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public long ManufacturerId { get; set; }

    public long CategoryId { get; set; }

    public int DosageFormId { get; set; }

    public string? Strength { get; set; }

    public int BaseUnitId { get; set; }

    public int PackUnitId { get; set; }

    public decimal PackSize { get; set; } = 1m;

    public decimal ReorderLevel { get; set; }

    public string? BarcodeNumber { get; set; }

    public string? RegistrationNumber { get; set; }

    public bool IsVatApplicable { get; set; }

    public decimal? VatRate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
}
