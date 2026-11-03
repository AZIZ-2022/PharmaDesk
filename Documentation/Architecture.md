# Architecture

## Solution layout

```
PharmaDesk.sln
global.json                  — pins the .NET SDK version used to build
Directory.Build.props        — shared compiler settings (nullable, implicit usings) across all projects

PharmaDesk.Domain/            — Entities, enums, value objects. No dependencies on any other project.
PharmaDesk.Application/       — DTOs, service interfaces, business/validation logic. Depends on Domain only.
PharmaDesk.Infrastructure/     — EF Core DbContext, entity configurations, DI registration, concrete service
                                implementations. Depends on Application + Domain.
PharmaDesk.Web/                — Razor MVC controllers/views, composition root (Program.cs). Depends on
                                Application + Infrastructure.
PharmaDesk.Tests.Unit/         — Fast tests with no external dependencies (Domain/Application logic).
PharmaDesk.Tests.Integration/  — Tests that require a real SQL Server instance (schema script, EF queries).

Database/PharmaDesk_Complete.sql — the single authoritative schema script (see DatabaseUpdateGuide.md).
Documentation/                — this folder.
```

Dependencies only ever point inward (Web/Infrastructure → Application → Domain). This keeps business rules —
batch allocation (FEFO), COGS calculation, tenant isolation — testable without a running web host or database,
and keeps EF Core/ASP.NET Core concerns out of the Domain and Application layers.

## EF Core vs. the SQL script

EF Core entity configurations exist for querying and dependency injection convenience. They are **not** the
deployment mechanism: `Database/PharmaDesk_Complete.sql` is hand-maintained as the single source of truth for
schema, and is what gets executed against a customer's SQL Server instance. EF migrations, if generated at all
during development, are a local drafting aid only and are never shipped.

## Multi-tenancy model

- **Tenant** = a pharmacy business (the customer). **Branch** = a physical location belonging to a Tenant.
  Implemented in Module 2 as `dbo.Tenants` / `dbo.Branches` — see `DatabaseDesign.md`.
- Today: one SQL Server database per installed customer, with exactly one Tenant row and one or more Branches —
  this is what gives "multi-branch" support now.
- A user's `TenantId` (mandatory) and `BranchId` (optional — null means not restricted to one branch) are baked
  into claims on their auth cookie at sign-in (`ApplicationUserClaimsPrincipalFactory`), read back via
  `ICurrentTenantContext` (interface in Application, implemented in Web from the HTTP context).
- Every tenant-scoped business table gets an EF Core global query filter comparing its `TenantId` column
  against `ICurrentTenantContext.TenantId`, with one safety rule: **no ambient tenant (background work, the
  identity seeder) means don't filter**, not "filter to nothing." `Branch` (Module 2) is the first entity with
  this filter; `MedicineCategory`, `Manufacturer`, and `Medicine` (Module 3), `Supplier`/`Customer` (Module 4),
  `Purchase`/`MedicineBatch` (Module 5), `StockAdjustment` (Module 6), `Sale` (Module 7),
  `SalesReturn`/`PurchaseReturn` (Module 8), `CustomerPayment`/`SupplierPayment` (Module 9), and
  `ExpenseCategory`/`Expense`/`CashDrawerSession` (Module 10) follow the same pattern, and every later
  tenant-scoped entity will too. Never trust a client-supplied tenant or branch ID instead of the claim —
  that's exactly the hole this mechanism exists to close.
- **`BranchId` is a plain column on `Purchase`/`MedicineBatch` (Module 5), not an EF query filter.** Unlike
  `TenantId`, branch is not auto-filtered — a user with no `BranchId` claim (e.g. Admin) needs to see stock
  and purchases across every branch, and later Reports/Transfers modules may need cross-branch visibility
  too. Any branch-level filtering happens explicitly per screen, deliberately kept separate from the
  tenant-isolation mechanism so that decision isn't locked in prematurely.
- **Module 5 creates `MedicineBatches`, not Module 6**, even though "Batch-wise Inventory" is Module 6's
  name. Batch number, expiry date, and cost are captured at the moment of purchase — they're not computed
  later — so the purchase-save transaction creates one `MedicineBatch` row per line item (quantity converted
  pack→base units via `Medicine.PackSize`, cost derived the same way). Module 6 owns everything downstream:
  stock-level views, low-stock/expiry alerts, and manual stock adjustments. Module 7 (POS) consumes batches
  FEFO-first when selling. This split was proposed in Module 5's plan and approved before implementation.
- **FEFO allocation (Module 7) is a pure function (`FefoAllocator`, Application layer), not inline
  controller logic.** Given a medicine's batches already sorted soonest-expiry-first and a requested
  quantity, it returns either a full per-batch allocation or a clean failure — there is no partial
  fulfillment. `SalesController.Complete` calls it once per line, inside the sale's database transaction,
  and never trusts a client-posted price: the authoritative unit price is always the soonest-expiring
  batch's price at the moment `Complete` runs, re-derived server-side even though the POS screen also
  shows a preview price when a line is first added (that preview is cosmetic only).
- **`SalesReturnAllocator` (Module 8) credits stock back to the exact original batches, not re-derived
  FEFO.** A return must put stock back where it actually came from (via the original sale's
  `SaleBatchAllocations`), not wherever FEFO would currently point — those can differ once other sales or
  purchases have happened in between. Symmetric in structure to `FefoAllocator` but walks a fixed list of
  original allocations instead of live batch state.
- **`StatementCalculator` (Module 9) is the one pure function behind the account statement views** —
  given an opening balance and a chronologically-sorted list of signed amounts, it returns the running
  balance after each one. `PaymentsController` builds the sorted entry list (Sales/Purchases due,
  Returns, Payments) and hands the amounts to this function rather than accumulating inline, so the
  arithmetic itself is unit-testable independent of the three-way query that assembles the entries.
- **`CashReconciliationCalculator` (Module 10) computes the cash-drawer expected-closing figure and
  variance as two pure functions**, called from `CashDrawerController.Close` on both the GET preview and
  the POST that actually closes the session — the POST always recomputes fresh from the database rather
  than trusting whatever expected value the GET page displayed, the same never-trust-the-client pattern
  `SalesController.Complete` established in Module 7.
- **`NotificationService` (Module 12) computes up to 4 aggregated notification items live on every
  request** (low stock, expired-with-stock batches, batches expiring within 30 days, cash drawers left
  open from a previous day) — no `Notifications` table, no read/unread/dismiss state, since these are
  ongoing conditions that a dismiss wouldn't resolve. It tags each item with the permission required to
  see it; the actual filtering by the viewing user's claims happens in `NotificationBellViewComponent`
  (the app's first View Component), not in the service, so the service itself stays testable without an
  `HttpContext` dependency.
- **The app creates backups but never executes `RESTORE DATABASE` itself (Module 13).** `BACKUP DATABASE`
  is safe to run against a live, in-use database; restoring needs exclusive access to the very database
  the running app depends on to serve the request that would trigger it, which is a fundamentally
  different risk profile. `BackupService.BuildRestoreScript` generates the exact `RESTORE DATABASE` T-SQL
  for a chosen backup file, for the admin to run in SSMS — the same "SSMS runs this, not the app"
  pattern already established for `Database/PharmaDesk_Complete.sql`.
- **`BackupBackgroundService` (Module 13) is the app's first `BackgroundService`** (a singleton hosted
  service): it creates its own DI scope per run (the standard pattern for a singleton needing scoped
  services like `PharmaDeskDbContext`) and checks hourly whether today's scheduled hour has passed,
  rather than sleeping for exactly 24 hours, so a restart doesn't permanently shift the schedule.
- **Backup file names arriving via a route parameter (Download/Delete) are validated by
  `BackupFileNameValidator`** (Application layer, pure function) before touching the filesystem — the one
  piece of genuinely security-relevant new logic in this module, rejecting anything with a path separator
  or `..` so a crafted filename can't read or delete outside the configured backup folder.
- **`BackupSettings:DirectoryPath` lives in appsettings.json, not the database or a UI setting** — it's a
  filesystem/deployment concern (which machine, which account's access), the same category as the
  connection string, not tenant business data.
- **`ProfitAndLossCalculator` (Module 11) is the pure function behind the COGS-based P&L report** —
  `ReportsController.ProfitAndLoss` computes Revenue and COGS by querying `SaleItems` and
  `SaleBatchAllocations` (the latter using each allocation's `CostPerBaseUnitAtSale`, i.e. the batch's
  actual cost at the time it was sold, not today's purchase price), and Expenses from `Expenses`, then
  hands the three totals to this function for the Gross Profit/Net Profit arithmetic. This is the first
  module to actually consume the COGS data every prior module since Module 5 was built to produce.
- Module 11 added no new tables — all four reports (Sales, Purchase, Profit & Loss, Inventory Valuation)
  read from tables already owned by earlier modules. Inventory Valuation is a live snapshot of current
  `MedicineBatches`, not a historical point-in-time reconstruction.
- **Security headers and login rate limiting (Module 14) are both plain ASP.NET Core middleware, not new
  packages.** The headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) are set by a
  small inline `app.Use(...)` delegate in `Program.cs`, placed before `UseHttpsRedirection`/`UseRouting` so
  they apply to every response including error pages. The login rate limiter uses the built-in
  `Microsoft.AspNetCore.RateLimiting` fixed-window limiter, registered as a named policy ("login") and
  applied via `[EnableRateLimiting("login")]` on `AccountController.Login`'s POST action only — every
  other endpoint is unaffected. `app.UseRateLimiter()` sits after `UseRouting()` (required, since the
  attribute-based policy needs endpoint metadata) and before `UseAuthentication()`, so a flooded login
  attempt is rejected before any authentication work runs.
- **The auth cookie's `SecurePolicy` is `Always` in Production and `SameAsRequest` elsewhere.**
  `ServiceCollectionExtensions.AddInfrastructure` now takes an `isProduction` flag (from
  `builder.Environment.IsProduction()` in `Program.cs`) specifically so this doesn't break logging in over
  plain HTTP during local development or before a fresh install's TLS certificate is configured — the same
  Dev/Prod branching pattern `UseHsts`/`UseDeveloperExceptionPage` already used.
- **Content-Security-Policy was deliberately not added in Module 14.** A real CSP without `unsafe-inline`
  would require refactoring every view with an inline `<script>` block (Purchases/Create, Sales/Pos,
  several Inventory/CashDrawer screens, the dashboard chart) to use nonces instead — a meaningful,
  view-by-view change that needs browser testing this environment can't do. Named as a follow-up, not
  attempted partially.
- Not every table is tenant-scoped: small, fixed reference vocabularies (`DosageForm`, `UnitOfMeasure` —
  Module 3) are seeded once globally and shared by every tenant, since there's no meaningful sense in which
  one pharmacy's "Tablet" differs from another's. The rule is: if a pharmacy would plausibly want to edit or
  extend the list itself, it's tenant-scoped (Categories, Manufacturers); if it's a fixed vocabulary the
  software defines, it's global.
- This is what makes a future shared-database SaaS deployment possible without a schema rewrite: swapping the
  claim source is the only change a hosted multi-tenant version would need.

## Conventions

- Surrogate keys: `BIGINT IDENTITY` (not GUID).
- Money: `DECIMAL(18,2)`, never `FLOAT`.
- Quantities and unit-conversion factors (e.g. `Medicine.PackSize`, `Medicine.ReorderLevel` — Module 3):
  also `DECIMAL(18,2)`, not `INT`. Liquids/partial units (ml, fractional packs) need the fractional part;
  reusing the money convention avoids introducing a second numeric convention for the same reason (never
  `FLOAT`).
- Timestamps: `DATETIME2(0)`, stored as UTC, named `CreatedAtUtc` / `ModifiedAtUtc`.
- Soft delete: `IsDeleted BIT` (+ `DeletedAtUtc` where applicable) on business tables — not on system tables
  like `DatabaseSchemaVersions`.
- Concurrency: `ROWVERSION` added to tables involved in concurrent financial operations (stock, payments) as
  those modules are built — not speculatively added everywhere.
