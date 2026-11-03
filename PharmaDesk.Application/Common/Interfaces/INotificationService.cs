namespace PharmaDesk.Application.Common.Interfaces;

public record NotificationItem(string Severity, string Message, string LinkUrl, string RequiredPermission);

public interface INotificationService
{
    Task<List<NotificationItem>> GetActiveNotificationsAsync();
}
