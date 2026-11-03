using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Payments;
using PharmaDesk.Application.Security;
using PharmaDesk.Domain.Payments;
using PharmaDesk.Infrastructure.Persistence;
using PharmaDesk.Web.Models.Partners;
using PharmaDesk.Web.Models.Payments;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly PharmaDeskDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;
    private readonly IAuditLogService _auditLog;

    public PaymentsController(PharmaDeskDbContext dbContext, ICurrentTenantContext currentTenant, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Payments.View)]
    public async Task<IActionResult> Receivables()
    {
        var customers = await _dbContext.Customers
            .Where(c => c.IsActive && c.CurrentBalance > 0)
            .OrderByDescending(c => c.CurrentBalance)
            .Select(c => new CustomerListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                IsCreditEligible = c.IsCreditEligible,
                CurrentBalance = c.CurrentBalance,
                IsWalkInDefault = c.IsWalkInDefault,
                IsActive = c.IsActive,
            })
            .ToListAsync();

        return View(customers);
    }

    [PermissionAuthorize(PermissionCodes.Payments.View)]
    public async Task<IActionResult> Payables()
    {
        var suppliers = await _dbContext.Suppliers
            .Where(s => s.IsActive && s.CurrentBalance > 0)
            .OrderByDescending(s => s.CurrentBalance)
            .Select(s => new SupplierListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                CurrentBalance = s.CurrentBalance,
                IsActive = s.IsActive,
            })
            .ToListAsync();

        return View(suppliers);
    }

    [PermissionAuthorize(PermissionCodes.Payments.Manage)]
    public async Task<IActionResult> RecordCustomerPayment(long customerId)
    {
        var customer = await _dbContext.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var model = new CustomerPaymentFormViewModel
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CurrentBalance = customer.CurrentBalance,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Payments.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordCustomerPayment(CustomerPaymentFormViewModel model)
    {
        var customer = await _dbContext.Customers.FindAsync(model.CustomerId);
        if (customer is null)
        {
            return NotFound();
        }

        model.CustomerName = customer.Name;
        model.CurrentBalance = customer.CurrentBalance;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var branchId = _currentTenant.BranchId ?? await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .FirstAsync();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var payment = new CustomerPayment
        {
            TenantId = tenantId,
            BranchId = branchId,
            CustomerId = customer.Id,
            Amount = model.Amount,
            PaymentMethod = model.PaymentMethod,
            PaymentDate = model.PaymentDate.ToDateTime(TimeOnly.MinValue),
            Notes = model.Notes,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.CustomerPayments.Add(payment);

        customer.CurrentBalance -= model.Amount;
        customer.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("CustomerPaymentRecorded", "CustomerPayment", payment.Id.ToString(), $"Recorded payment of {model.Amount:N2} from {customer.Name}");

        return RedirectToAction(nameof(Receivables));
    }

    [PermissionAuthorize(PermissionCodes.Payments.Manage)]
    public async Task<IActionResult> RecordSupplierPayment(long supplierId)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(supplierId);
        if (supplier is null)
        {
            return NotFound();
        }

        var model = new SupplierPaymentFormViewModel
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            CurrentBalance = supplier.CurrentBalance,
        };

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Payments.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordSupplierPayment(SupplierPaymentFormViewModel model)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(model.SupplierId);
        if (supplier is null)
        {
            return NotFound();
        }

        model.SupplierName = supplier.Name;
        model.CurrentBalance = supplier.CurrentBalance;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_currentTenant.TenantId is not long tenantId)
        {
            return Problem("Unable to determine the current pharmacy (tenant). Please sign in again.");
        }

        var branchId = _currentTenant.BranchId ?? await _dbContext.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .FirstAsync();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var payment = new SupplierPayment
        {
            TenantId = tenantId,
            BranchId = branchId,
            SupplierId = supplier.Id,
            Amount = model.Amount,
            PaymentMethod = model.PaymentMethod,
            PaymentDate = model.PaymentDate.ToDateTime(TimeOnly.MinValue),
            Notes = model.Notes,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.SupplierPayments.Add(payment);

        supplier.CurrentBalance -= model.Amount;
        supplier.ModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogAsync("SupplierPaymentRecorded", "SupplierPayment", payment.Id.ToString(), $"Recorded payment of {model.Amount:N2} to {supplier.Name}");

        return RedirectToAction(nameof(Payables));
    }

    [PermissionAuthorize(PermissionCodes.Payments.View)]
    public async Task<IActionResult> CustomerStatement(long customerId)
    {
        var customer = await _dbContext.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var entries = new List<StatementEntryViewModel>();

        var sales = await _dbContext.Sales
            .Where(s => s.CustomerId == customerId)
            .Select(s => new { s.Id, s.SaleDate, Due = s.TotalAmount - s.AmountPaid })
            .ToListAsync();

        entries.AddRange(sales.Select(s => new StatementEntryViewModel
        {
            Date = s.SaleDate,
            Description = $"Sale #{s.Id}",
            Amount = s.Due,
        }));

        var returns = await _dbContext.SalesReturns
            .Where(r => r.CustomerId == customerId)
            .Select(r => new { r.SaleId, r.ReturnedAtUtc, r.ReturnAmount })
            .ToListAsync();

        entries.AddRange(returns.Select(r => new StatementEntryViewModel
        {
            Date = r.ReturnedAtUtc,
            Description = $"Return from Sale #{r.SaleId}",
            Amount = -r.ReturnAmount,
        }));

        var payments = await _dbContext.CustomerPayments
            .Where(p => p.CustomerId == customerId)
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync();

        entries.AddRange(payments.Select(p => new StatementEntryViewModel
        {
            Date = p.PaymentDate,
            Description = "Payment received",
            Amount = -p.Amount,
        }));

        entries = entries.OrderBy(e => e.Date).ToList();

        var runningBalances = StatementCalculator.ComputeRunningBalances(customer.OpeningBalance, entries.Select(e => e.Amount).ToList());
        for (var i = 0; i < entries.Count; i++)
        {
            entries[i].RunningBalance = runningBalances[i];
        }

        var model = new StatementViewModel
        {
            PartyId = customer.Id,
            PartyName = customer.Name,
            OpeningBalance = customer.OpeningBalance,
            CurrentBalance = customer.CurrentBalance,
            Entries = entries,
        };

        return View(model);
    }

    [PermissionAuthorize(PermissionCodes.Payments.View)]
    public async Task<IActionResult> SupplierStatement(long supplierId)
    {
        var supplier = await _dbContext.Suppliers.FindAsync(supplierId);
        if (supplier is null)
        {
            return NotFound();
        }

        var entries = new List<StatementEntryViewModel>();

        var purchases = await _dbContext.Purchases
            .Where(p => p.SupplierId == supplierId)
            .Select(p => new { p.Id, p.PurchaseDate, Due = p.TotalAmount - p.AmountPaid })
            .ToListAsync();

        entries.AddRange(purchases.Select(p => new StatementEntryViewModel
        {
            Date = p.PurchaseDate,
            Description = $"Purchase #{p.Id}",
            Amount = p.Due,
        }));

        var returns = await _dbContext.PurchaseReturns
            .Where(r => r.SupplierId == supplierId)
            .Select(r => new { r.PurchaseId, r.ReturnedAtUtc, r.ReturnAmount })
            .ToListAsync();

        entries.AddRange(returns.Select(r => new StatementEntryViewModel
        {
            Date = r.ReturnedAtUtc,
            Description = $"Return from Purchase #{r.PurchaseId}",
            Amount = -r.ReturnAmount,
        }));

        var payments = await _dbContext.SupplierPayments
            .Where(p => p.SupplierId == supplierId)
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync();

        entries.AddRange(payments.Select(p => new StatementEntryViewModel
        {
            Date = p.PaymentDate,
            Description = "Payment made",
            Amount = -p.Amount,
        }));

        entries = entries.OrderBy(e => e.Date).ToList();

        var runningBalances = StatementCalculator.ComputeRunningBalances(supplier.OpeningBalance, entries.Select(e => e.Amount).ToList());
        for (var i = 0; i < entries.Count; i++)
        {
            entries[i].RunningBalance = runningBalances[i];
        }

        var model = new StatementViewModel
        {
            PartyId = supplier.Id,
            PartyName = supplier.Name,
            OpeningBalance = supplier.OpeningBalance,
            CurrentBalance = supplier.CurrentBalance,
            Entries = entries,
        };

        return View(model);
    }
}
