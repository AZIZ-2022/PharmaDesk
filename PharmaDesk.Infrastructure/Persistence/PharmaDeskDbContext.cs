using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Domain.Catalog;
using PharmaDesk.Domain.Finance;
using PharmaDesk.Domain.Organization;
using PharmaDesk.Domain.Partners;
using PharmaDesk.Domain.Payments;
using PharmaDesk.Domain.Purchasing;
using PharmaDesk.Domain.Returns;
using PharmaDesk.Domain.Sales;
using PharmaDesk.Domain.Security;
using PharmaDesk.Domain.SystemEntities;
using PharmaDesk.Infrastructure.Identity;

namespace PharmaDesk.Infrastructure.Persistence;

public class PharmaDeskDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    long,
    IdentityUserClaim<long>,
    IdentityUserRole<long>,
    IdentityUserLogin<long>,
    IdentityRoleClaim<long>,
    IdentityUserToken<long>>
{
    private readonly ICurrentTenantContext _currentTenant;

    public PharmaDeskDbContext(DbContextOptions<PharmaDeskDbContext> options, ICurrentTenantContext currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<DatabaseSchemaVersion> DatabaseSchemaVersions => Set<DatabaseSchemaVersion>();

    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    public DbSet<DosageForm> DosageForms => Set<DosageForm>();

    public DbSet<MedicineCategory> MedicineCategories => Set<MedicineCategory>();

    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();

    public DbSet<Medicine> Medicines => Set<Medicine>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();

    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<SaleBatchAllocation> SaleBatchAllocations => Set<SaleBatchAllocation>();

    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();

    public DbSet<SalesReturnBatchCredit> SalesReturnBatchCredits => Set<SalesReturnBatchCredit>();

    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();

    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<CashDrawerSession> CashDrawerSessions => Set<CashDrawerSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserRole<long>>(b => b.ToTable("UserRoles", "dbo"));
        modelBuilder.Entity<IdentityUserClaim<long>>(b => b.ToTable("UserClaims", "dbo"));
        modelBuilder.Entity<IdentityUserLogin<long>>(b => b.ToTable("UserLogins", "dbo"));
        modelBuilder.Entity<IdentityRoleClaim<long>>(b => b.ToTable("RoleClaims", "dbo"));
        modelBuilder.Entity<IdentityUserToken<long>>(b => b.ToTable("UserTokens", "dbo"));

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PharmaDeskDbContext).Assembly);

        // No ambient tenant (background/seeding work outside a request) means "don't filter" —
        // callers inside a real web request always have one, via the TenantId claim.
        modelBuilder.Entity<Branch>().HasQueryFilter(b =>
            !_currentTenant.TenantId.HasValue || b.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<MedicineCategory>().HasQueryFilter(c =>
            !_currentTenant.TenantId.HasValue || c.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Manufacturer>().HasQueryFilter(m =>
            !_currentTenant.TenantId.HasValue || m.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Medicine>().HasQueryFilter(m =>
            !_currentTenant.TenantId.HasValue || m.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Supplier>().HasQueryFilter(s =>
            !_currentTenant.TenantId.HasValue || s.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Customer>().HasQueryFilter(c =>
            !_currentTenant.TenantId.HasValue || c.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Purchase>().HasQueryFilter(p =>
            !_currentTenant.TenantId.HasValue || p.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<MedicineBatch>().HasQueryFilter(b =>
            !_currentTenant.TenantId.HasValue || b.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<StockAdjustment>().HasQueryFilter(a =>
            !_currentTenant.TenantId.HasValue || a.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Sale>().HasQueryFilter(s =>
            !_currentTenant.TenantId.HasValue || s.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<SalesReturn>().HasQueryFilter(r =>
            !_currentTenant.TenantId.HasValue || r.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<PurchaseReturn>().HasQueryFilter(r =>
            !_currentTenant.TenantId.HasValue || r.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<CustomerPayment>().HasQueryFilter(p =>
            !_currentTenant.TenantId.HasValue || p.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<SupplierPayment>().HasQueryFilter(p =>
            !_currentTenant.TenantId.HasValue || p.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<ExpenseCategory>().HasQueryFilter(c =>
            !_currentTenant.TenantId.HasValue || c.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<Expense>().HasQueryFilter(e =>
            !_currentTenant.TenantId.HasValue || e.TenantId == _currentTenant.TenantId);

        modelBuilder.Entity<CashDrawerSession>().HasQueryFilter(s =>
            !_currentTenant.TenantId.HasValue || s.TenantId == _currentTenant.TenantId);
    }
}
