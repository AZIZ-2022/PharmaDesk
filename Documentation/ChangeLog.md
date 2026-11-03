# Change Log

## 2026-11-02 — Module 0: Project Foundation and Architecture

- Scaffolded the solution: `PharmaDesk.Domain`, `PharmaDesk.Application`, `PharmaDesk.Infrastructure`,
  `PharmaDesk.Web`, `PharmaDesk.Tests.Unit`, `PharmaDesk.Tests.Integration`.
- Added `BaseEntity`, `DatabaseSchemaVersion`, `ApplicationSetting` to the Domain layer.
- Added `PharmaDeskDbContext`, EF Core entity configurations, and `IDatabaseHealthService` /
  `DatabaseHealthService` to Infrastructure; wired via `AddInfrastructure(IConfiguration)`.
- Added Serilog (console + rolling file) logging, configured from `appsettings.json`.
- Built the Razor dashboard shell: responsive sidebar (Bootstrap 5 offcanvas), topbar, placeholder metric
  cards, and a live database-connectivity status banner.
- Created `Database/PharmaDesk_Complete.sql` v0.1.0: idempotent, version-gated script that creates the
  database if missing and bootstraps `dbo.DatabaseSchemaVersions` + `dbo.ApplicationSettings`.
- Added `PharmaDesk.Tests.Unit` (one passing test) and `PharmaDesk.Tests.Integration` (schema bootstrap smoke
  test — requires a live SQL Server instance, not run in this environment).

## 2026-11-02 — Module 1: Authentication, Users, Roles and Permissions

- Added ASP.NET Core Identity with `BIGINT` keys (`ApplicationUser`, `ApplicationRole`), tables renamed to
  Users/Roles/UserRoles/UserClaims/UserLogins/RoleClaims/UserTokens.
- Added a custom `Permissions` catalog + `RolePermissions` grant table (Domain: `Permission`;
  Infrastructure: `RolePermission`), independent of Identity's own role-claims mechanism.
- Added claims-based fine-grained authorization: `ApplicationUserClaimsPrincipalFactory` bakes a user's
  effective permission codes into their auth cookie at sign-in; `PermissionAuthorizeAttribute` +
  `PermissionPolicyProvider` + `PermissionAuthorizationHandler` (Web/Security) enforce them per-action.
- Added `IdentitySeeder`: seeds exactly one default admin account on a brand-new database (roles/permissions
  themselves are SQL-seeded data, not app-seeded, since only the password hash needs Identity's own hasher).
  Logs the generated temporary password once; the account must change its password before reaching anything
  else (enforced by middleware in `Program.cs`).
- Added `IAuditLogService` / `AuditLogService` and `ICurrentUserContext` / `CurrentUserContext`, logging auth
  and admin actions to `AuditLogs`.
- Added `AccountController` (Login/Logout/ChangePassword/AccessDenied), `UsersController`, `RolesController`
  (with a permission checkbox matrix), and their views; added a login-specific layout (`_LoginLayout.cshtml`).
- Login now fails gracefully (friendly message, not a 500) if the database is unreachable, matching the
  Module 0 dashboard's degrade-don't-crash pattern; also added the Development-only exception page that was
  missing from Module 0's `Program.cs`.
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.2.0: Identity tables, Permissions,
  RolePermissions, AuditLogs, and baseline Admin/Pharmacist/Cashier role seed data.
- Added `PharmaDesk.Tests.Unit` coverage for the temporary-password generator (meets Identity's password
  policy, not deterministic). The login/seeding flow against a real database still needs verification once
  the SQL script is injected — see `ModuleStatus.md`.

## 2026-11-02 — Module 2: Tenant, Branch and Organization Management

- Added `Tenant` and `Branch` (Domain: `PharmaDesk.Domain.Organization`), `TenantConfiguration` /
  `BranchConfiguration` (Infrastructure), and `DbSet<Tenant>` / `DbSet<Branch>` on `PharmaDeskDbContext`.
- Added `TenantId` (mandatory) and `BranchId` (optional) to `ApplicationUser`.
- Added an EF Core global query filter on `Branch`, scoped to `ICurrentTenantContext.TenantId` when an
  ambient tenant is resolvable, and unfiltered otherwise (background/seeding work) — verified with two
  `PharmaDesk.Tests.Unit` tests against the EF Core InMemory provider (`BranchTenantQueryFilterTests`), since
  this logic lives inside an EF expression tree and can't be unit-tested any other way.
- Added `ICurrentTenantContext` (Application) / `CurrentTenantContext` (Web, reads the `TenantId`/`BranchId`
  claims); `ApplicationUserClaimsPrincipalFactory` now adds those claims at sign-in.
- Updated `IdentitySeeder`: the seeded admin account now gets `TenantId` set from the one Tenant row seeded by
  SQL — the normal run order is "inject SQL, then start the app," so the Tenant exists before the admin user
  does, and the seeder would otherwise violate the new `NOT NULL` constraint.
- Added `TenantController` (single-record Pharmacy Profile settings page) and `BranchesController`
  (list/create/edit/toggle-active, refusing to deactivate the last active branch) with their views; extended
  the User Create/Edit forms with an optional Branch dropdown.
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.3.0: `Tenants`/`Branches` tables, the
  placeholder Tenant + Main Branch seed, the `Users.TenantId`/`BranchId` `ALTER TABLE` + backfill + `NOT NULL`
  + FK sequence, and the three new Organization permissions granted to Admin.
- The `Users.TenantId` backfill against a database that already has rows (e.g. a previously-seeded admin
  account) has not been exercised against a real SQL Server in this environment — see `ModuleStatus.md`.

## 2026-11-03 — Module 3: Medicine Master Data Management

- Added `DosageForm` and `UnitOfMeasure` (Domain: `PharmaDesk.Domain.Catalog`) as global, non-tenant-scoped
  reference lists, seeded once by SQL (10 dosage forms, 10 units) — not editable through the UI in this
  module, since they're a fixed vocabulary rather than tenant-managed data.
- Added `MedicineCategory`, `Manufacturer`, and `Medicine` (same namespace), all tenant-scoped with the same
  EF Core global query filter pattern Module 2 established for `Branch`. `Manufacturer` here is the drug
  company and is explicitly distinct from a Module 4 Supplier (the wholesaler).
- `Medicine` carries a base-unit/pack-unit/`PackSize` conversion (e.g. 10 Tablets per Strip) for Module 6/7 to
  consume later, plus `ReorderLevel`, optional `BarcodeNumber`/`RegistrationNumber`, and an
  `IsVatApplicable`/`VatRate` pair. `Strength` is free text by design (tablet/syrup/injection strength formats
  don't share one structured shape).
- Added `MedicinesController` (list with brand/generic search, create/edit, deactivate/reactivate — never hard
  delete), `MedicineCategoriesController`, `ManufacturersController`, and their views; extended the sidebar
  nav and `_ViewImports.cshtml`.
- Added `Medicines.View` / `Medicines.Manage` permissions, covering the Category/Manufacturer sub-screens too
  (they are not independent top-level permission domains).
- Added `PharmaDesk.Tests.Unit` coverage (`MedicineTenantQueryFilterTests`) verifying the query filter on
  `Medicine` specifically, including that the two separate foreign keys to `UnitsOfMeasure` (BaseUnitId,
  PackUnitId) persist correctly — the one place in this module two FKs point at the same target table.
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.4.0: `DosageForms`/`UnitsOfMeasure` (global,
  seeded), `MedicineCategories`/`Manufacturers`/`Medicines` tables, and the new Medicines permissions granted
  to Admin.
- Documented a new convention in `Architecture.md`: quantity/conversion-factor columns are `DECIMAL(18,2)`,
  reusing the money convention, for Modules 5–9 to follow.
- As with every prior module, the actual CRUD screens have not been exercised against a real SQL Server in
  this environment — only build, unit tests, and live HTTP smoke tests confirming correct auth redirects.

## 2026-11-03 — Module 4: Supplier and Customer Management

- Added `Supplier` and `Customer` (Domain: `PharmaDesk.Domain.Partners`), both tenant-scoped with the same EF
  Core global query filter pattern as `Branch`/`Medicine`. `Supplier` is explicitly distinct from Module 3's
  `Manufacturer` (wholesaler vs. drug company).
- Both carry `OpeningBalance` (captured once) and `CurrentBalance` (initialized from it); edits to
  `OpeningBalance` afterward adjust `CurrentBalance` by delta rather than overwriting it, so future
  transactional activity (Module 5/7/9) is never silently erased by a master-data correction.
- `Customer` adds `IsCreditEligible`/`CreditLimit` and `IsWalkInDefault` — SQL seeds one "Walk-in Customer"
  row per existing tenant so Module 7's POS always has a fallback for anonymous cash sales.
  `CustomersController.ToggleActive` refuses to deactivate a Walk-in-default row, the same protective pattern
  Module 2 used for the last active branch.
- Added `SuppliersController`, `CustomersController` (list with name/phone search, create/edit,
  deactivate/reactivate — never hard delete) and their views; extended the sidebar nav and
  `_ViewImports.cshtml`.
- Added `Suppliers.View`/`Suppliers.Manage` and `Customers.View`/`Customers.Manage` permissions.
- Added `PharmaDesk.Tests.Unit` coverage (`PartnerTenantQueryFilterTests`) verifying the tenant query filter
  on both new entities.
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.5.0: `Suppliers`/`Customers` tables, the
  seeded Walk-in Customer per tenant, and the four new permissions granted to Admin.
- As with every prior module, the CRUD screens and the Walk-in Customer seed have not been exercised against
  a real SQL Server in this environment — only build, unit tests, and a live HTTP smoke test confirming
  correct auth redirects on `/Suppliers` and `/Customers`.

## 2026-11-03 — Module 5: Purchase Management

- Added `Purchase`, `PurchaseItem`, `MedicineBatch` (Domain: `PharmaDesk.Domain.Purchasing`), tenant-scoped
  with the established EF Core query filter pattern (`Purchase`, `MedicineBatch`; `PurchaseItem` is scoped
  transitively through its parent Purchase).
- Added `PurchaseBatchCalculator` (Application layer, pure static methods) for the pack-to-base-unit
  quantity/cost conversion, so the one risky piece of arithmetic in this module is covered by a real unit
  test (`MedicineBatchConversionTests`) rather than a test that just re-derives the same formula.
- `PurchasesController.Create` saves a Purchase header, its PurchaseItems, and one `MedicineBatch` per item,
  then increases `Supplier.CurrentBalance` by the unpaid amount — all inside a single database transaction.
  Purchases are intentionally create-and-view only; there is no edit screen (see `BusinessRules.md`).
- **Key design decision, proposed and approved in this module's plan:** Module 5 creates `MedicineBatches`
  directly, even though Module 6 is named "Batch-wise Inventory" — batch/expiry/cost are captured at
  purchase time, not computed later. Module 6 will own stock-level views, alerts, and adjustments on top of
  these rows, not their creation.
- `BranchId` is captured on `Purchases`/`MedicineBatches` as a required column but is **not** auto-filtered
  by an EF query filter the way `TenantId` is — branch-level filtering is left to individual screens,
  deliberately, so it isn't locked in before later modules need it.
- Added `PurchasesController` (Index, Create — header + dynamic line items via plain JS add/remove rows with
  client-side reindexing, Details — read-only) and views; extended the sidebar nav and `_ViewImports.cshtml`.
- Added `Purchases.View`/`Purchases.Manage` permissions.
- Added `PharmaDesk.Tests.Unit` coverage: `MedicineBatchConversionTests` (the pack→base-unit conversion
  math, via `PurchaseBatchCalculator`) and `PurchaseTenantQueryFilterTests` (tenant isolation on `Purchase`
  and `MedicineBatch`).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.6.0: `Purchases`/`PurchaseItems`/
  `MedicineBatches` tables and the new Purchases permissions granted to Admin.
- As with every prior module, the purchase-save transaction has not been exercised against a real SQL
  Server in this environment — only build, unit tests, and a live HTTP smoke test confirming correct auth
  redirects on `/Purchases`, `/Purchases/Create`, and `/Purchases/Details/{id}`.

## 2026-11-03 — Module 6: Batch-wise Inventory

- Added `StockAdjustment` (Domain: `PharmaDesk.Domain.Purchasing`), tenant-scoped with the established EF
  Core query filter pattern — the audit trail for manual corrections to `MedicineBatches.QuantityRemaining`.
- Added `StockAdjustmentCalculator` (Application layer, pure static method) for the negative-quantity guard,
  covered by a real unit test (`StockAdjustmentCalculatorTests`) rather than inline controller logic that
  would only be exercisable through an HTTP request.
- Added `InventoryController`: `StockOverview` (aggregates `MedicineBatches.QuantityRemaining` per medicine,
  compared against `Medicine.ReorderLevel`, branch-filterable), `Batches` (filterable list, exhausted
  batches hidden by default), `ExpiryAlerts` (expired or expiring within a hardcoded 90-day threshold), and
  `Adjust` (GET/POST — writes the `StockAdjustment` row and updates `QuantityRemaining` in one transaction,
  guarded against going negative). None of these create batches — that remains Module 5's job, per the
  scope split proposed and approved in Module 5's plan.
- Added `Inventory.View`/`Inventory.Adjust` permissions, extended the sidebar nav and `_ViewImports.cshtml`
  (also added `@using Microsoft.AspNetCore.Mvc.Rendering` globally, needed for `List<SelectListItem>` casts
  from `ViewData` in the new branch-filter dropdowns).
- Added `PharmaDesk.Tests.Unit` coverage: `StockAdjustmentCalculatorTests` (the negative-quantity guard,
  both allowed and rejected cases) and `StockAdjustmentTenantQueryFilterTests` (tenant isolation on the new
  entity).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.7.0: `StockAdjustments` table and the new
  Inventory permissions granted to Admin. No changes to `MedicineBatches` itself.
- As with every prior module, the adjustment transaction has not been exercised against a real SQL Server
  in this environment — only build, unit tests, and a live HTTP smoke test confirming correct auth redirects
  on all four new routes.

## 2026-11-03 — Module 7: POS and Sales Management

- Added `Sale`, `SaleItem`, `SaleBatchAllocation` (Domain: `PharmaDesk.Domain.Sales`); `Sale` is
  tenant-scoped with the established query filter pattern.
- Added `FefoAllocator` (Application layer, pure functions: `TryAllocate` and `GetQuotedUnitPrice`) — the
  most heavily unit-tested piece of logic in the app so far (`FefoAllocatorTests`: exact-match, multi-batch
  and three-batch splits, insufficient-stock rejection, zero-quantity batches skipped, fractional
  quantities, zero-quantity requests, and the price-quote edge cases).
- **Key pricing design decision, proposed and approved in Module 7's plan:** a sale line quotes one price
  from the soonest-expiring batch with stock, even if fulfilling the quantity draws from a second,
  differently-costed batch. `SaleBatchAllocations` keeps the real per-batch cost for COGS (Module 11)
  without the customer ever seeing more than one price per line.
- Added `SalesController`: `Index`/`Details` (receipt, plain browser-printable HTML, `Layout = null`),
  `Pos` (the register screen), `SearchMedicines` (the app's first AJAX/JSON endpoint — a debounced
  typeahead over active medicines, returning brand/generic name, FEFO-quoted price, and available quantity
  for the selected branch), and `Complete` (POST — runs `FefoAllocator` per line inside one DB transaction,
  enforces customer credit eligibility/limit, decrements the consumed batches, updates
  `Customer.CurrentBalance`, and never trusts the client-posted preview price for the actual money math).
- `Pos.cshtml` is the first screen with a dynamic row count driven by both server-rendered rows (on
  validation-failure redisplay) and client-added rows (via search-result clicks) sharing one
  reindex/recalculate JS pipeline, extending the dynamic-row pattern Module 5 started for Purchases.
- Added `Sales.View`/`Sales.Manage` permissions, extended the sidebar nav and `_ViewImports.cshtml`.
- Added `PharmaDesk.Tests.Unit` coverage: `FefoAllocatorTests` (9 cases) and `SaleTenantQueryFilterTests`
  (tenant isolation on `Sale`).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.8.0: `Sales`/`SaleItems`/
  `SaleBatchAllocations` tables and the new Sales permissions granted to Admin.
- No thermal-printer/ESC-POS integration — explicitly out of scope without physical hardware to test
  against; the receipt view is browser-print only.
- As with every prior module, the sale-completion transaction (FEFO allocation, stock decrement, customer
  balance update) has not been exercised against a real SQL Server in this environment — only build, unit
  tests, and a live HTTP smoke test confirming correct auth redirects on all four new routes.

## 2026-11-03 — Module 8: Sales and Purchase Returns

- Added `SalesReturn`, `SalesReturnBatchCredit`, `PurchaseReturn` (Domain: `PharmaDesk.Domain.Returns`);
  `SalesReturn`/`PurchaseReturn` are tenant-scoped with the established query filter pattern.
- Added `SalesReturnAllocator` (Application layer, pure function, symmetric in structure to
  `FefoAllocator`): credits a returned quantity back to the exact original `SaleBatchAllocations` in their
  original order, rather than re-deriving FEFO against today's batch state. Covered by
  `SalesReturnAllocatorTests` (single/partial/multi-batch credits, exceeding-total rejection, fractional
  quantities, zero-quantity allocations skipped).
- **Key scope decision, proposed and approved in Module 8's plan:** a sale line can only be returned once,
  enforced by a unique constraint on `SalesReturns.SaleItemId` as well as an app check — not just multiple
  partial returns dribbling in over time. Purchase returns have no such limit (each just checks against
  the batch's current `QuantityRemaining`).
- Added `ReturnsController`: `SalesReturns`/`PurchaseReturns` (lists), `NewSalesReturn`/`NewPurchaseReturn`
  (GET/POST) — each POST runs in one transaction (return row + batch credit/decrement if applicable +
  balance update + audit log). A sales return's `IsRestocked` flag controls whether
  `SalesReturnBatchCredits` rows are created at all (financial-only returns, e.g. a damaged item, create
  none).
- Added "Return" links on `Sales/Details` (per line, hidden once already returned) and `Purchases/Details`
  (per line, hidden once the batch has no stock left) — extended `SaleDetailsItemViewModel`/
  `PurchaseDetailsItemViewModel` and their controllers' queries to carry the IDs and state those links need.
- **Balances can now go negative** (`Customer.CurrentBalance` via sales returns, `Supplier.CurrentBalance`
  via purchase returns) — correct ledger behavior meaning money is owed back, not a bug.
- Added `Returns.View`/`Returns.Manage` permissions, extended the sidebar nav and `_ViewImports.cshtml`.
- Added `PharmaDesk.Tests.Unit` coverage: `SalesReturnAllocatorTests` (6 cases) and
  `ReturnsTenantQueryFilterTests` (tenant isolation on both new entities).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.9.0: `SalesReturns`/
  `SalesReturnBatchCredits`/`PurchaseReturns` tables (including the unique index enforcing the one-return
  rule) and the new Returns permissions granted to Admin.
- As with every prior module, the return transactions have not been exercised against a real SQL Server
  in this environment — only build, unit tests, and a live HTTP smoke test confirming correct auth
  redirects on all four new routes.

## 2026-11-03 — Module 9: Due and Payment Management

- Added `CustomerPayment`, `SupplierPayment` (Domain: `PharmaDesk.Domain.Payments`), tenant-scoped with
  the established query filter pattern — standalone payments against the running balance, not matched to
  a specific sale/purchase invoice (a scope decision proposed and approved in the plan).
- Added `StatementCalculator` (Application layer, pure function): computes the running balance after each
  entry given an opening balance and a chronologically-sorted list of signed amounts. Covered by
  `StatementCalculatorTests` (empty list, single debit/credit, mixed entries, and the
  balance-going-negative case).
- Added `PaymentsController`: `Receivables`/`Payables` (due lists — customers/suppliers with
  `CurrentBalance > 0`, reusing the existing `CustomerListItemViewModel`/`SupplierListItemViewModel` rather
  than new ones), `RecordCustomerPayment`/`RecordSupplierPayment` (GET/POST, balance update + audit log in
  one transaction, no overpayment cap), and `CustomerStatement`/`SupplierStatement` (read-only ledger
  combining Sales/Purchases' due portion, Returns, and Payments chronologically via `StatementCalculator`).
- Added `Payments.View`/`Payments.Manage` permissions, extended the sidebar nav ("Due & Payments") and
  `_ViewImports.cshtml`.
- Added `PharmaDesk.Tests.Unit` coverage: `StatementCalculatorTests` (5 cases) and
  `PaymentTenantQueryFilterTests` (tenant isolation on both new entities).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.10.0: `CustomerPayments`/
  `SupplierPayments` tables and the new Payments permissions granted to Admin.
- Every planned mutation path for `Customer.CurrentBalance`/`Supplier.CurrentBalance` (purchases/sales,
  returns, and now standalone payments) is implemented as of this module.
- As with every prior module, the payment-recording transactions have not been exercised against a real
  SQL Server in this environment — only build, unit tests, and a live HTTP smoke test confirming correct
  auth redirects on all six new routes.

## 2026-11-03 — Module 10: Expense and Cash Management

- Added `ExpenseCategory`, `Expense`, `CashDrawerSession` (Domain: `PharmaDesk.Domain.Finance`),
  tenant-scoped with the established query filter pattern.
- **Key scope decision, proposed and approved in Module 10's plan:** `Expense` supports Edit and Void
  (`ToggleActive`), breaking from the Purchases/Sales/Returns create-view-only precedent — an expense
  doesn't move stock or any party's balance, so there's nothing else to reverse.
- Added `CashReconciliationCalculator` (Application layer, pure functions: `ComputeExpectedClosing`,
  `ComputeVariance`). The expected-closing formula sums cash `Sales`/`CustomerPayments` as inflows and
  cash `Expenses`/`SupplierPayments` as outflows against the opening amount; it deliberately excludes
  `SalesReturns`/`PurchaseReturns`, which never recorded a cash/non-cash settlement marker — flagged as a
  real accuracy gap, not silently wrong. Covered by `CashReconciliationCalculatorTests`.
- Added `ExpenseCategoriesController` (mirrors `MedicineCategoriesController`), `ExpensesController` (list
  with date-range/category filter, create/edit/void), and `CashDrawerController` (`Index` — per-branch
  open/closed status + history, `Open`, `Close` — GET shows a preview expected figure, POST always
  recomputes it fresh server-side rather than trusting the preview, same pattern
  `SalesController.Complete` established in Module 7). Only one open session per branch at a time,
  enforced at the app level.
- Added `Expenses.View`/`Expenses.Manage` and `CashDrawer.View`/`CashDrawer.Manage` permissions, extended
  the sidebar nav and `_ViewImports.cshtml`.
- Added `PharmaDesk.Tests.Unit` coverage: `CashReconciliationCalculatorTests` (4 cases) and
  `FinanceTenantQueryFilterTests` (tenant isolation on all three new entities).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.11.0: `ExpenseCategories`/`Expenses`/
  `CashDrawerSessions` tables and the four new permissions granted to Admin.
- As with every prior module, the expense and cash-drawer transactions have not been exercised against a
  real SQL Server in this environment — only build, unit tests, and a live HTTP smoke test confirming
  correct auth redirects on all five new routes.

## 2026-11-03 — Module 11: Reports and Analytics

- Added `ProfitAndLossCalculator` (Application layer, pure function: `Compute(revenue, cogs, expenses)` →
  Gross Profit / Net Profit). Covered by `ProfitAndLossCalculatorTests` (profit, loss, and zero-activity
  cases).
- Added `ReportsController` with four read-only reports, no new tables:
  - `SalesReport` / `PurchaseReport` — date range + branch (and supplier, for purchases) filters,
    grouped by medicine, with a currency grand total only (quantity totals across differently-united
    medicines wouldn't mean anything, so they're shown per row, not summed).
  - `ProfitAndLoss` — Revenue from `SaleItems`, COGS from `SaleBatchAllocations.CostPerBaseUnitAtSale`
    (the actual batch cost at the time of sale — the master spec's explicit requirement, and the reason
    that field was snapshotted back in Module 7), Expenses from active `Expenses` records in range.
  - `InventoryValuation` — a live snapshot (current `MedicineBatches.QuantityRemaining × 
    PurchaseCostPerBaseUnit`), explicitly not a historical point-in-time reconstruction.
  - Each report has a `?format=csv` export (manual CSV building, no new library).
- Added `Reports.View` (single permission — the module makes no writes), extended the sidebar nav and
  `_ViewImports.cshtml`.
- Added `PharmaDesk.Tests.Unit` coverage: `ProfitAndLossCalculatorTests` (4 cases).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.12.0 — no new tables, just the
  `Reports.View` permission seed granted to Admin.
- As with every prior module, the report queries have only been checked against an empty schema (build +
  live HTTP smoke test confirming correct auth redirects on all four routes) — not verified to produce
  correct numbers against real transactions in a live database.

## 2026-11-03 — Module 12: Dashboard and Notifications

- Added `INotificationService`/`NotificationService` (Infrastructure, same pattern as `IAuditLogService`):
  computes up to 4 aggregated, live notification items (low/out-of-stock medicine count, expired batches
  still holding stock, batches expiring within 30 days, cash drawers left open from a previous day) from
  existing tables. No new table — deliberately not persisted, no dismiss/read state (see
  `BusinessRules.md`). Each item carries the permission code required to see it; filtering by the
  viewer's actual claims happens in the view layer, not the service.
- Added `NotificationBellViewComponent` — **the app's first View Component** — rendered in the topbar via
  `_Layout.cshtml`, filtering the service's items by `HttpContext.User.HasClaim(...)` before rendering a
  badge count + dropdown.
- Rewrote `HomeController.Index`/`DashboardViewModel`/`Views/Home/Index.cshtml` to replace Module 0's
  placeholder metric cards with real KPIs (Today's/Month's Revenue, Total Receivable/Payable, Low Stock,
  Expiring Soon) and a 14-day sales trend **Chart.js line chart — the first real use of a library that's
  been in the stated tech stack since Module 0 but unused until now**. Both the cards and the chart are
  gated per-card by the viewing user's existing permissions, and the dashboard defaults to the user's own
  branch (from their `BranchId` claim) with an override dropdown, same pattern as every other
  branch-aware screen.
- Registered `INotificationService` in `ServiceCollectionExtensions.AddInfrastructure`.
- No new unit tests this module — unlike every prior module, there's no new pure-function business logic
  to extract (the notification aggregation and dashboard KPIs are DB-query assembly, the same category as
  Module 6's `StockOverview`, which also wasn't unit-tested beyond a live smoke test).
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.13.0 — no new tables or permissions; the
  version row exists purely so the module history stays complete.
- **This module's live testing is more limited than every prior one**: the dashboard has required
  authentication since Module 0, and logging in at all requires a database, so the live smoke test could
  only confirm `/` still redirects to login without crashing (DI resolves) — not even the graceful-failure
  path of the new KPI queries or the `NotificationBell` component has been exercised here.

## 2026-11-03 — Module 13: Backup and Restore

- Added `BackupFileNameValidator` (Application layer, pure function): rejects anything that isn't a bare
  `name.bak` with no path separators or `..`, before any file-system access. Covered by
  `BackupFileNameValidatorTests` (valid names, path traversal via `../`/`..\\`, subdirectories, absolute
  paths, wrong extension, empty/null).
- Added `IBackupService`/`BackupService` (Infrastructure): `CreateBackupAsync` runs
  `BACKUP DATABASE [name] TO DISK = ... WITH INIT, STATS = 10` against the configured folder;
  `ListBackups`/`DeleteBackup`/`ApplyRetention`/`GetFullPath`/`BuildRestoreScript` round out file
  management. The database name is read from the active connection
  (`_dbContext.Database.GetDbConnection().Database`) rather than hardcoded, so it matches whatever the
  connection string actually points at.
- **Key safety decision, proposed and approved in Module 13's plan: the app creates backups but never
  executes `RESTORE DATABASE` itself.** `BuildRestoreScript` generates the exact T-SQL (stop the app,
  `ALTER DATABASE ... SET SINGLE_USER`, `RESTORE DATABASE ... WITH REPLACE`, `SET MULTI_USER`) for the
  admin to run in SSMS — the same "SSMS runs this, not the app" pattern `Database/PharmaDesk_Complete.sql`
  already established.
- Added `BackupBackgroundService` — **the app's first `BackgroundService`** — checking hourly whether
  today's scheduled hour (`BackupSettings:DailyBackupHourUtc`) has passed and no backup has run today,
  creating its own DI scope per run, then applying retention. Registered alongside
  `services.Configure<BackupSettings>(...)` in `ServiceCollectionExtensions.AddInfrastructure`.
- Added `BackupSettings` (Infrastructure POCO bound from a new `BackupSettings` section in
  `appsettings.json`: `DirectoryPath`, `RetentionCount`, `DailyBackupHourUtc`) — a filesystem/deployment
  concern, deliberately not a database or UI setting, same category as the connection string.
- Added `BackupController` (`Index`, `CreateBackup`, `Download` via `PhysicalFile`, `Delete`,
  `RestoreInstructions`) and views; extended the sidebar nav and `_ViewImports.cshtml`.
- Added `Backup.View`/`Backup.Manage` permissions.
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.14.0 — no new tables (backup files live
  on disk, not in the database), just the two new permissions granted to Admin.
- **This module's live smoke test was unusually conclusive**: on startup, `BackupBackgroundService`
  attempted an immediate backup (no prior run existed this session), the `BACKUP DATABASE` call failed
  because no SQL Server is reachable here, the failure was caught and logged rather than crashing the
  host, and the app kept serving requests normally afterward — confirmed directly from the log output,
  not just inferred. The actual `BACKUP DATABASE`/`RESTORE DATABASE` execution and real file I/O against
  a live SQL Server remain unverified, same standing limitation as every prior module.

## 2026-11-03 — Module 14: Testing, Security Hardening and Deployment

- **Audited every controller in the solution before writing any code**: confirmed `[ValidateAntiForgeryToken]`
  on all `[HttpPost]` actions, `[Authorize]`/`[PermissionAuthorize]` on every controller/sensitive action,
  the login redirect already guarded via `Url.IsLocalUrl()`, the one raw-SQL call (Module 13's
  `BACKUP DATABASE`) already parameterized where it matters, both `Html.Raw()` usages confirmed to wrap only
  app-generated JSON (never user input), admin-set/reset passwords confirmed to always go through Identity's
  hasher, and no leftover `TODO`/`FIXME`/`HACK` comments anywhere. All reported as verified findings.
- Added a small response-header middleware in `Program.cs` (`X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`), placed before
  `UseHttpsRedirection`/`UseRouting` so it applies to every response. Verified live: all three headers
  present on an actual response from the running app.
- `ServiceCollectionExtensions.AddInfrastructure` now takes an `isProduction` flag; the auth cookie's
  `SecurePolicy` is `Always` in Production and `SameAsRequest` elsewhere, so this doesn't break logging in
  over plain HTTP during local development or before a fresh install's TLS certificate exists.
- Added a login-endpoint rate limiter (.NET's built-in `Microsoft.AspNetCore.RateLimiting`, no new
  package): a named "login" fixed-window policy (10 requests/minute/IP), applied via
  `[EnableRateLimiting("login")]` on `AccountController.Login`'s POST action only, with
  `app.UseRateLimiter()` placed after `UseRouting()` and before `UseAuthentication()`. **Verified live**:
  scripted 12 rapid POSTs to `/Account/Login` — the first 10 came back as normal failed-login responses
  (200), the 11th and 12th came back `429 Too Many Requests`, exactly as designed.
- **Content-Security-Policy deliberately not added** — a real CSP needs every inline `<script>` block
  across the app refactored to nonces, which needs browser testing this environment can't do. Named as a
  follow-up, not attempted partially.
- Rewrote `Documentation/DeploymentGuide.md` into a full production runbook: recommended same-machine
  SQL-Server-plus-web-app topology, `dotnet publish` + IIS hosting (with a Windows Service alternative
  noted), HTTPS certificate guidance, keeping the connection string out of a committed `appsettings.json`
  via environment-variable overrides, backup-folder permission requirements for both the app pool identity
  and the SQL Server service account, the first-run sequence (inject SQL → start app → find the seeded
  admin's temporary password in the log file → log in → forced password change → fill in the real Pharmacy
  Profile), a recap of what's hardened vs. what remains the operator's responsibility, and the
  update/upgrade procedure.
- Ran the full 74-test unit suite one final time (unchanged from Module 13 — this module added no new
  pure-function logic to test, only middleware/configuration) and did a final clean solution build.
- Updated `Database/PharmaDesk_Complete.sql` to schema version 0.15.0 — no schema changes, a version-row
  only, closing out all 15 modules (0–14) from the original master development plan.
- **This is the most directly, live-verifiable module in the entire project** — the security headers and
  the rate limiter were both checked against the actual running app's HTTP responses, not inferred from a
  redirect or a log line, because neither depends on a database connection. The deeper code audit was a
  read-through, not a penetration test; a real external security review before shipping to a paying
  customer is still advisable, and no module's actual data-layer behavior has been proven against a live
  SQL Server anywhere in this project — see every module's own entry above for the specific gap.
