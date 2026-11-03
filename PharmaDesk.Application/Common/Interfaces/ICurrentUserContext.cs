namespace PharmaDesk.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    long? UserId { get; }

    string? IpAddress { get; }
}
