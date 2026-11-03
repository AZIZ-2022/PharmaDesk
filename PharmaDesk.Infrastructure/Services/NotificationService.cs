using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Infrastructure.Persistence;

namespace PharmaDesk.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly PharmaDeskDbContext _dbContext;

    public NotificationService(PharmaDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<NotificationItem>> GetActiveNotificationsAsync()
    {
        var notifications = new List<NotificationItem>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var batchTotals = await _dbContext.MedicineBatches
            .GroupBy(b => b.MedicineId)
            .Select(g => new { MedicineId = g.Key, Total = g.Sum(b => b.QuantityRemaining) })
            .ToDictionaryAsync(x => x.MedicineId, x => x.Total);

        var medicines = await _dbContext.Medicines
            .Where(m => m.IsActive)
            .Select(m => new { m.Id, m.ReorderLevel })
            .ToListAsync();

        var lowStockCount = medicines.Count(m => (batchTotals.TryGetValue(m.Id, out var qty) ? qty : 0) <= m.ReorderLevel);

        if (lowStockCount > 0)
        {
            notifications.Add(new NotificationItem(
                "warning",
                $"{lowStockCount} medicine(s) are low on stock or out of stock.",
                "/Inventory/StockOverview",
                PermissionCodes.Inventory.View));
        }

        var expiredCount = await _dbContext.MedicineBatches
            .CountAsync(b => b.QuantityRemaining > 0 && b.ExpiryDate < today);

        if (expiredCount > 0)
        {
            notifications.Add(new NotificationItem(
                "danger",
                $"{expiredCount} batch(es) have expired and still have stock remaining.",
                "/Inventory/ExpiryAlerts",
                PermissionCodes.Inventory.View));
        }

        var expiringSoonCount = await _dbContext.MedicineBatches
            .CountAsync(b => b.QuantityRemaining > 0 && b.ExpiryDate >= today && b.ExpiryDate <= today.AddDays(30));

        if (expiringSoonCount > 0)
        {
            notifications.Add(new NotificationItem(
                "warning",
                $"{expiringSoonCount} batch(es) are expiring within 30 days.",
                "/Inventory/ExpiryAlerts",
                PermissionCodes.Inventory.View));
        }

        var openFromPreviousDay = await _dbContext.CashDrawerSessions
            .CountAsync(s => s.ClosedAtUtc == null && s.SessionDate < today);

        if (openFromPreviousDay > 0)
        {
            notifications.Add(new NotificationItem(
                "danger",
                $"{openFromPreviousDay} cash drawer session(s) from a previous day are still open.",
                "/CashDrawer",
                PermissionCodes.CashDrawer.View));
        }

        return notifications;
    }
}
