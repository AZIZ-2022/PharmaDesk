namespace PharmaDesk.Domain.Common;

public abstract class BaseEntity
{
    public long Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
}
