using Microsoft.AspNetCore.Mvc;
using PharmaDesk.Application.Common.Interfaces;

namespace PharmaDesk.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationBellViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var all = await _notificationService.GetActiveNotificationsAsync();
        var visible = all.Where(n => HttpContext.User.HasClaim("permission", n.RequiredPermission)).ToList();

        return View(visible);
    }
}
