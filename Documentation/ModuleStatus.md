# Module Status

Last updated: 2026-11-03. Schema version: **0.15.0**. **All 15 modules (0–14) from the original master
development plan are complete.**

## Completed

- **Module 0 — Project Foundation and Architecture.** Solution/project scaffolding, `BaseEntity` convention,
  `PharmaDeskDbContext`, Serilog logging, the Razor dashboard shell (sidebar/topbar), and
  `Database/PharmaDesk_Complete.sql` v0.1.0 (DatabaseSchemaVersions + ApplicationSettings bootstrap).
- **Module 1 — Authentication, Users, Roles and Permissions.** ASP.NET Core Identity (BIGINT keys, tables
  renamed to Users/Roles/UserRoles/UserClaims/UserLogins/RoleClaims/UserTokens), a custom Permissions /
  RolePermissions catalog layered on top for fine-grained authorization, claims-based permission checks
  (`PermissionAuthorizeAttribute`), login/logout, forced password change on first login, account
  lockout, AuditLogs, and User/Role management screens. Schema version 0.2.0.
- **Module 2 — Tenant, Branch and Organization Management.** `Tenants`/`Branches` tables, `Users.TenantId`
  (mandatory) / `Users.BranchId` (optional) added via `ALTER TABLE` + backfill, an EF Core global query filter
  scoping `Branch` queries to the caller's tenant claim, a Pharmacy Profile settings screen, Branch management,
  and Branch assignment on the User form. Schema version 0.3.0.
- **Module 3 — Medicine Master Data Management.** `DosageForms`/`UnitsOfMeasure` global reference lists
  (seeded), tenant-scoped `MedicineCategories`/`Manufacturers`/`Medicines` catalog tables, base-unit/pack-unit
  conversion fields on `Medicines` (for Module 6/7 to consume), VAT applicability flag, and management screens
  for all three with search on the Medicines list. Schema version 0.4.0.
- **Module 4 — Supplier and Customer Management.** `Suppliers` and `Customers` tables (tenant-scoped), both
  with `OpeningBalance`/`CurrentBalance` fields initialized at creation for Module 5/7/9 to build on, a
  seeded default "Walk-in Customer" per tenant (protected from deactivation) for Module 7's POS to use for
  anonymous sales, optional credit eligibility/limit on Customers, and search-enabled list screens for both.
  Schema version 0.5.0.
- **Module 5 — Purchase Management.** `Purchases`/`PurchaseItems`/`MedicineBatches` tables. Recording a
  purchase creates one `MedicineBatch` per line item (quantity converted from pack units to base units via
  `Medicine.PackSize`) and increases `Supplier.CurrentBalance` by the unpaid amount — the first real
  mutation of that field. Purchases are create-and-view only (no edit screen); a single DB transaction
  covers the header, all line items, all batches, and the supplier balance update together. Schema version
  0.6.0.
- **Module 6 — Batch-wise Inventory.** Read-only stock overview (aggregated per medicine, compared against
  `Medicine.ReorderLevel`), a filterable batch list, and an expiry-alerts view — all built on top of Module
  5's `MedicineBatches` with no new master data. Adds the one write path this module owns: manual stock
  adjustments (damage/expired/count correction), recorded in a new `StockAdjustments` audit table, guarded
  so `QuantityRemaining` can never go negative. Schema version 0.7.0.
- **Module 7 — POS/Sales Management.** `Sales`/`SaleItems`/`SaleBatchAllocations` tables. The POS screen
  (typeahead medicine search → cart → complete) runs FEFO allocation server-side at save time via a new
  `FefoAllocator`, quoting one unit price per line from the soonest-expiring batch even when fulfilling it
  spans multiple batches, decrements the consumed batches' `QuantityRemaining`, and — the first real
  mutation of `Customer.CurrentBalance` — enforces credit eligibility/limit before allowing any due amount.
  Sales are create-and-view only, same as Purchases. Schema version 0.8.0.
- **Module 8 — Sales and Purchase Returns.** `SalesReturns`/`SalesReturnBatchCredits`/`PurchaseReturns`
  tables. A sales return (initiated from a receipt line) can optionally restock the exact original batches
  via a new `SalesReturnAllocator`, and always reduces `Customer.CurrentBalance` (which can go negative,
  meaning a cash refund is owed). A purchase return (from a purchase line) always decrements its batch and
  reduces `Supplier.CurrentBalance`. A given sale line can only be returned once — a deliberate v1 scope
  limit, enforced by a DB unique constraint as well as an app check. Schema version 0.9.0.
- **Module 9 — Due and Payment Management.** `CustomerPayments`/`SupplierPayments` tables — standalone
  payments against the running balance, not matched to a specific sale/purchase invoice. Accounts
  Receivable/Payable list views (customers/suppliers with an outstanding balance), payment recording
  forms, and a per-party statement view combining sales/purchases, returns, and payments chronologically
  with a running balance (via a new `StatementCalculator`), starting from `OpeningBalance`. Schema version
  0.10.0.
- **Module 10 — Expense and Cash Management.** `ExpenseCategories`/`Expenses`/`CashDrawerSessions` tables.
  Expenses support Edit and Void — a deliberate departure from the create-view-only precedent, since
  nothing else depends on an expense record. Cash drawer sessions (one open per branch at a time) compute
  an expected closing cash figure from cash Sales/CustomerPayments/Expenses/SupplierPayments via a new
  `CashReconciliationCalculator`, compared against the physically-counted actual to produce a variance.
  Deliberately excludes Returns' cash effect (no settlement-method marker exists on them). Schema version
  0.11.0.
- **Module 11 — Reports and Analytics.** No new tables — four read-only reports over existing data: Sales
  Report and Purchase Report (date-ranged, grouped by medicine), Profit & Loss (Revenue − COGS = Gross
  Profit; Gross Profit − Expenses = Net Profit, with COGS computed from `SaleBatchAllocations`' actual
  batch cost via a new `ProfitAndLossCalculator`, not today's purchase price — the master spec's
  explicit requirement), and Inventory Valuation (a live stock-value snapshot, not historical). Each
  report has a plain CSV export. Schema version 0.12.0 (permission seed only).
- **Module 12 — Dashboard and Notifications.** No schema changes. `HomeController.Index` replaced Module
  0's placeholder with real KPI cards (Today's/Month's Revenue, Total Receivable/Payable, Low Stock,
  Expiring Soon), a 14-day sales Chart.js line chart, and a branch filter — each card/chart gated by the
  viewing user's existing permissions. A new `NotificationService` + the app's first View Component
  (`NotificationBell`, topbar) surface up to 4 aggregated, live-computed notifications (low stock,
  expired batches with stock, batches expiring within 30 days, cash drawers left open from a previous
  day) — deliberately not persisted, no dismiss/read state, since these are ongoing conditions, not
  one-time events. Schema version 0.13.0 (version-row only, no permissions or tables).
- **Module 13 — Backup and Restore.** No new tables. `IBackupService`/`BackupService` runs
  `BACKUP DATABASE` to a configured folder (`BackupSettings:DirectoryPath` in appsettings.json — a
  deployment/filesystem concern, not tenant data), lists/downloads/deletes `.bak` files, and applies a
  retention count. A `BackupBackgroundService` runs this automatically once a day. **The app deliberately
  does not execute `RESTORE DATABASE` itself** — restoring needs exclusive access to the database the
  running app itself depends on, so the app instead generates a ready-to-run restore script for SSMS.
  Backup file names from routes are validated by a dedicated `BackupFileNameValidator` against path
  traversal before any file-system operation. Schema version 0.14.0 (permission seed only).
- **Module 14 — Testing, Security Hardening and Deployment.** No schema changes beyond a version-history
  row. A codebase-wide security audit (documented in `ChangeLog.md`) confirmed CSRF tokens, permission
  checks, the open-redirect guard, raw-SQL safety, and password handling were all already correct across
  every prior module, with no gaps found. Three concrete fixes were added: security response headers
  (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`), a production-only always-secure auth
  cookie, and a login-endpoint rate limiter (10 attempts/minute/IP) as defense-in-depth beyond Identity's
  per-account lockout — the rate limiter was verified live (10 attempts succeeded, the 11th/12th returned
  429). `Documentation/DeploymentGuide.md` was completed into a real production runbook. Schema version
  0.15.0 (version-row only).

## Current

- All 15 modules from the original master development plan are complete. No module is in progress.

## Pending

None from the original master plan. Any further work (the planned .NET 10 upgrade once that SDK is
available, a future Content-Security-Policy pass, tenant-configurable backup/expiry settings, etc.) would
be a new, explicitly-scoped request, not a continuation of the existing module order.

## Known issues / environment notes

- **Module 14's security fixes were the most directly verifiable work in the whole project**, precisely
  because none of them depend on a database: the security headers were confirmed present on a live
  response, and the login rate limiter was confirmed to allow exactly 10 requests/minute and reject the
  11th/12th with 429 — both checked against the actually running app, not inferred. The deeper audit
  (CSRF/permission/SQL-injection/password-handling review) was a code read-through, not a penetration
  test against a live attacker-reachable instance — a real external security review before shipping to a
  paying customer is still advisable, especially for anything this document couldn't exercise (there is
  no live SQL Server in this environment, so no module's actual data-layer behavior has been proven end
  to end — see every module's entry above for the specific gaps).
- Development machine has only the .NET 9 SDK installed; the project targets .NET 9 for now with a planned
  upgrade to .NET 10 LTS once that SDK is available (explicit decision, 2026-11-02).
- No SQL Server engine instance is reachable from the development machine. By agreement, database setup and
  testing against a live instance is done by the project owner after each module's SQL script update — the
  integration test suite (`PharmaDesk.Tests.Integration`) requires `PHARMADESK_TEST_MASTER_CONNECTION` /
  `PHARMADESK_TEST_CONNECTION` to point at a real instance and has not been run end-to-end in this environment.
  This also means the full login flow (Module 1), the default admin seeding (now including its Tenant
  assignment, Module 2), the Users.TenantId backfill against a database that already has rows, and the
  Medicines/MedicineCategories/Manufacturers CRUD screens (Module 3), and the Suppliers/Customers CRUD
  screens plus the seeded Walk-in Customer (Module 4), the Purchase create/save transaction (Module 5), the
  stock-adjustment transaction (Module 6), the POS sale-completion transaction (Module 7), the return
  transactions (Module 8), the payment-recording transactions (Module 9), and the expense/cash-drawer
  transactions (Module 10) have only been verified to *fail gracefully* without a database, not to succeed
  with one — that still needs verification once the SQL script is injected. The same applies to Module
  11's reports: with no real data in a live database, the report queries have only been checked for
  compiling/executing against an empty schema, not for producing correct numbers against real transactions.
- **Module 12's dashboard and notification bell could not be exercised live even for graceful degradation**,
  unlike every other module — the dashboard has required authentication since Module 0, and logging in at
  all requires a database. The live smoke test only confirmed `/` still redirects to login without
  crashing (i.e. DI registration for `NotificationService` and the new `HomeController` constructor
  dependencies resolves). The actual KPI queries, chart data, and the `NotificationBell` view component
  have not been exercised at all in this environment, not even their failure path.
- **Module 13's scheduled backup was actually exercised live and confirmed to fail gracefully**: on
  startup, `BackupBackgroundService` attempted an immediate backup (since no prior run existed this
  session), the `BACKUP DATABASE` call failed because no SQL Server is reachable, the failure was caught
  and logged, and the app kept serving requests normally. This is stronger verification than most prior
  modules got, precisely because the background service runs independently of any authenticated request.
  The actual `BACKUP DATABASE`/`RESTORE DATABASE` SQL, and real file creation/download, remain unverified
  against a live SQL Server.
- **The app does not execute `RESTORE DATABASE` itself (Module 13), flagged and approved before
  building.** Restoring requires exclusive access to the database the running app depends on; the app
  instead generates the exact restore script for SSMS. This mirrors how `Database/PharmaDesk_Complete.sql`
  itself is already "run this in SSMS," not something the app runs on itself.
- `BackupSettings:DirectoryPath` (Module 13) must be a path both the web app's process and the SQL Server
  service account can read/write — simplest when SQL Server and the web app run on the same machine (the
  expected single-pharmacy on-prem deployment). This is a real deployment-topology assumption, not
  automatically true for every possible installation.
- The tenant-scoped-vs-global split in Module 3 (Categories/Manufacturers are per-tenant; DosageForms/
  UnitsOfMeasure are global seeded lists) is a judgment call flagged in `BusinessRules.md` — worth revisiting
  if it turns out pharmacies need custom units.
- `Supplier.CurrentBalance`/`Customer.CurrentBalance` (Module 4) are initialized from `OpeningBalance` and
  adjusted by delta if `OpeningBalance` is later edited, and moved by purchases/sales (Module 5/7), returns
  (Module 8), and now standalone payments (Module 9) — every planned mutation path for these two fields is
  now implemented.
- **No invoice-level payment matching (Module 9), flagged in its plan and approved before building.** A
  customer/supplier payment reduces the overall running balance; it is not allocated against a specific
  sale/purchase. Realistic for how a small pharmacy actually manages this, but means there's no
  "which invoice does this payment cover" traceability if that's ever needed.
- No cap against overpayment on `RecordCustomerPayment`/`RecordSupplierPayment` (Module 9) — same
  negative-balance-is-valid precedent Module 8 established for returns.
- **Expenses support Edit and Void (Module 10), unlike Purchases/Sales/Returns' create-view-only
  precedent.** Flagged and approved: an expense doesn't move stock or any party's balance, so there's
  nothing else to reverse — the usual concern behind that precedent doesn't apply here.
- **The cash-drawer "expected closing" formula excludes Sales/Purchase Returns (Module 10), flagged and
  approved before building.** Returns never recorded a cash/non-cash settlement marker, so there's no
  clean field to filter cash refunds by. A pharmacy doing a lot of cash refunds will see a less accurate
  variance than one that doesn't — a real gap, documented rather than silently wrong. Fixing it would mean
  adding a settlement-method field to Module 8's `SalesReturns`/`PurchaseReturns`, not a Module 10 change.
- **Inventory Valuation (Module 11) is a live snapshot, not a historical "as of date X" valuation.**
  Confirmed and approved in the plan — reconstructing stock levels as they stood on a past date would need
  to replay transaction history, which is real complexity intentionally scoped out of this module.
- Module 11's four reports have no automated/scheduled delivery, no charts, and no aging-bucket analysis
  on dues — all explicitly out of scope per the approved plan (charts belong to Module 12's Dashboard).
- **Notifications (Module 12) are live-computed, not persisted — no read/unread/dismiss state, flagged
  and approved before building.** The underlying conditions (low stock, an expired batch, a drawer left
  open) don't go away when dismissed, so a dismiss button would be actively misleading. The bell always
  reflects current state on every page load.
- Dashboard cards and notification items are gated by the viewing user's existing permissions (e.g.
  Receivable/Payable only shown with `Payments.View`) — no new permissions were added for this, Module 12
  only reads claims that already exist.
- **The POS pricing design decision (Module 7), flagged in its plan and approved before building:** a sale
  line quotes one price from the soonest-expiring batch with stock, even if fulfilling the quantity spans
  into a second batch with a different cost. The customer always pays one consistent price per line; COGS
  stays accurate per actual batch via `SaleBatchAllocations`. If this ever needs to change (e.g. splitting
  a line's price across batches), it's a `SalesController.Complete` change, not a schema change.
- **No thermal-printer/ESC-POS integration (Module 7)** — the receipt (`Sales/Details`) is a plain
  browser-printable HTML page. Explicitly out of scope without physical hardware to test against.
- **A sale line can only be returned once (Module 8), never multiple partial returns against the same
  line.** Flagged in Module 8's plan and approved — revisiting it means redesigning the per-batch
  returned-quantity bookkeeping, not a quick patch. Purchase returns have no such limit.
- `Customer.CurrentBalance`/`Supplier.CurrentBalance` can go negative as of Module 8 (a sales/purchase
  return reduces what's owed, and can overshoot into "the pharmacy/supplier owes money back"). This is
  correct ledger behavior, not a bug — the system tracks the net balance; the cash exchange itself happens
  outside the system.
- The Module 5/6 scope split (Module 5 creates `MedicineBatches` at purchase time; Module 6 owns stock-level
  views/alerts/adjustments on top of them) was presented and approved as part of Module 5's plan — flagged
  here since the module name "Batch-wise Inventory" could otherwise suggest batches belong entirely to
  Module 6.
- The expiry-alert threshold (90 days) is hardcoded in `InventoryController`, not tenant-configurable yet —
  `ApplicationSettings` (Module 0) already exists for exactly this if it's ever needed, so it's a small
  follow-up rather than a redesign.
- Git is not installed on the development machine, so this project is not yet under version control.
- The seeded Tenant row ("Your Pharmacy Name (update in Settings)") is a placeholder — the admin should fill
  in real business details via the Pharmacy Profile screen after first login.
