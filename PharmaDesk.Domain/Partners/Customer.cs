namespace PharmaDesk.Domain.Partners;

public class Customer
{
    public long Id { get; set; }

    public long TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsCreditEligible { get; set; }

    public decimal? CreditLimit { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public bool IsWalkInDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
}
