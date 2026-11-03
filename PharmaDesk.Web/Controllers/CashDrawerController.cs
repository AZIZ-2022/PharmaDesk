using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Finance;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Finance;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Finance;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class CashDrawerController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public CashDrawerController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.CashDrawer.View)]
    public async Task<IActionResult> Index()
    {
        var branches = await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();

        var openSessions = await _dbContext.CashDrawerSessions
            .Where(s => s.ClosedAtUtc == null)
            .ToListAsync();

        var statuses = branches.Select(b =>
        {
            var open = openSessions.FirstOrDefault(s => s.BranchId == b.Id);
            return new CashDrawerBranchStatusViewModel
            {
                BranchId = b.Id,
                BranchName = b.Name,
                OpenSessionId = open?.Id,
                OpeningCashAmount = open?.OpeningCashAmount,
                OpenedAtUtc = open?.OpenedAtUtc,
            };
        }).ToList();

        var history = await _dbContext.CashDrawerSessions
            .Where(s => s.ClosedAtUtc != null)
            .Join(_dbContext.Branches, s => s.BranchId, b => b.Id, (s, b) => new CashDrawerHistoryItemViewModel
            {
                Id = s.Id,
                BranchName = b.Name,
                SessionDate = s.SessionDate,
                OpeningCashAmount = s.OpeningCashAmount,
                ClosingCashAmount = s.ClosingCashAmount,
                ExpectedClosingCashAmount = s.ExpectedClosingCashAmount,
                Variance = s.Variance,
                OpenedAtUtc = s.OpenedAtUtc,
                ClosedAtUtc = s.ClosedAtUtc,
            })
            .OrderByDescending(s => s.ClosedAtUtc)
            .Take(50)
            .ToListAsync();

        var model = new CashDrawerIndexViewModel
        {
            BranchStatuses = statuses,
            History = history,
        };

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.CashDrawer.Manage)]
    public async Task<IActionResult> Open()
    {
        var model = new CashDrawerOpenFormViewModel();
        await PopulateAvailableBranchesAsync(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.CashDrawer.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(CashDrawerOpenFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAvailableBranchesAsync(model);
            return View(model);
        }

        var alreadyOpen = await _dbContext.CashDrawerSessions
            .AnyAsync(s => s.BranchId == model.BranchId && s.ClosedAtUtc == null);

        if (alreadyOpen)
        {
            ModelState.AddModelError(string.Empty, "This branch already has an open cash drawer session. Close it before opening a new one.");
            await PopulateAvailableBranchesAsync(model);
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var session = new CashDrawerSession
        {
            TenantId = tenantId,
            BranchId = model.BranchId,
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            OpeningCashAmount = model.OpeningCashAmount,
            Notes = model.Notes,
            OpenedAtUtc = DateTime.UtcNow,
        };

        _dbContext.CashDrawerSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("CashDrawerOpened", "CashDrawerSession", session.Id.ToString(), $"Opened cash drawer with {model.OpeningCashAmount:N2}");

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.CashDrawer.Manage)]
    public async Task<IActionResult> Close(long sessionId)
    {
        var session = await _dbContext.CashDrawerSessions.FindAsync(sessionId);
        if (session is null || session.ClosedAtUtc != null)
        {
            return NotFound();
        }

        var branch = await _dbContext.Branches.FindAsync(session.BranchId);
        var expected = await ComputeExpectedClosingAsync(session);

        var model = new CashDrawerCloseFormViewModel
        {
            SessionId = session.Id,
            BranchName = branch?.Name ?? string.Empty,
            SessionDate = session.SessionDate,
            OpeningCashAmount = session.OpeningCashAmount,
            ExpectedClosingCashAmount = expected,
            ClosingCashAmount = expected,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.CashDrawer.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(CashDrawerCloseFormViewModel model)
    {
        var session = await _dbContext.CashDrawerSessions.FindAsync(model.SessionId);
        if (session is null || session.ClosedAtUtc != null)
        {
            return NotFound();
        }

        var branch = await _dbContext.Branches.FindAsync(session.BranchId);
        model.BranchName = branch?.Name ?? string.Empty;
        model.SessionDate = session.SessionDate;
        model.OpeningCashAmount = session.OpeningCashAmount;

        // Never trust a client-posted expected amount - always recompute fresh.
        var expected = await ComputeExpectedClosingAsync(session);
        model.ExpectedClosingCashAmount = expected;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        session.ClosingCashAmount = model.ClosingCashAmount;
        session.ExpectedClosingCashAmount = expected;
        session.Variance = CashReconciliationCalculator.ComputeVariance(model.ClosingCashAmount, expected);
        session.Notes = model.Notes;
        session.ClosedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditLog.LogAsync("CashDrawerClosed", "CashDrawerSession", session.Id.ToString(), $"Closed cash drawer: counted {model.ClosingCashAmount:N2}, expected {expected:N2}, variance {session.Variance:N2}");

        return RedirectToAction(nameof(Index));
    }

    private async Task<decimal> ComputeExpectedClosingAsync(CashDrawerSession session)
    {
        var startOfDay = session.SessionDate.ToDateTime(TimeOnly.MinValue);
        var endOfDay = startOfDay.AddDays(1);

        var cashSalesTotal = await _dbContext.Sales
            .Where(s => s.BranchId == session.BranchId && s.PaymentMethod == "Cash" && s.SaleDate >= startOfDay && s.SaleDate < endOfDay)
            .SumAsync(s => (decimal?)s.AmountPaid) ?? 0m;

        var cashCustomerPaymentsTotal = await _dbContext.CustomerPayments
            .Where(p => p.BranchId == session.BranchId && p.PaymentMethod == "Cash" && p.PaymentDate >= startOfDay && p.PaymentDate < endOfDay)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var cashExpensesTotal = await _dbContext.Expenses
            .Where(e => e.BranchId == session.BranchId && e.IsActive && e.PaymentMethod == "Cash" && e.ExpenseDate >= startOfDay && e.ExpenseDate < endOfDay)
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        var cashSupplierPaymentsTotal = await _dbContext.SupplierPayments
            .Where(p => p.BranchId == session.BranchId && p.PaymentMethod == "Cash" && p.PaymentDate >= startOfDay && p.PaymentDate < endOfDay)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return CashReconciliationCalculator.ComputeExpectedClosing(
            session.OpeningCashAmount,
            cashSalesTotal,
            cashCustomerPaymentsTotal,
            cashExpensesTotal,
            cashSupplierPaymentsTotal);
    }

    private async Task PopulateAvailableBranchesAsync(CashDrawerOpenFormViewModel model)
    {
        var openBranchIds = await _dbContext.CashDrawerSessions
            .Where(s => s.ClosedAtUtc == null)
            .Select(s => s.BranchId)
            .ToListAsync();

        model.AvailableBranches = await _dbContext.Branches
            .Where(b => b.IsActive && !openBranchIds.Contains(b.Id))
            .OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
            .ToListAsync();
    }
}
