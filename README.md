# PharmaDesk

**PharmaDesk** is a commercial, multi-branch pharmacy management system built for pharmacies in
Bangladesh, starting with its first customer: a medium-sized retail pharmacy. It covers the full
day-to-day operation of a pharmacy — medicine catalog, batch-wise inventory with FEFO, purchasing,
point-of-sale, returns, supplier/customer dues, expenses, cash reconciliation, reporting, and backups —
built on a schema designed to support a future multi-tenant SaaS deployment without a rewrite.

All 15 planned modules are complete. See [Documentation/ModuleStatus.md](Documentation/ModuleStatus.md)
for the detailed, module-by-module build history, including every design decision and known limitation.

## Features

### Access Control
- ASP.NET Core Identity with a custom fine-grained **permission system** layered on top of roles — every
  controller action is gated by a specific permission (`Module.View` / `Module.Manage`), not just a
  coarse role check.
- Forced password change on first login for the seeded admin account; account lockout after repeated
  failed attempts; a login-endpoint rate limit as defense-in-depth on top of that.

### Organization
- **Tenant** (the pharmacy business) and **Branch** (physical locations) management, with every
  business table carrying `TenantId`/`BranchId` from day one.

### Medicine Catalog
- Brand/generic name, manufacturer, category, dosage form, and a base-unit/pack-unit conversion (e.g.
  10 tablets per strip) so medicines can be purchased by the box and sold as single loose pieces —
  common practice at retail pharmacies in Bangladesh.

### Suppliers & Customers
- Supplier (wholesaler) and Customer records with running balances, credit eligibility/limits, and a
  protected default "Walk-in Customer" for anonymous cash sales.

### Purchasing & Batch-wise Inventory
- Recording a purchase creates **batch-level stock records** with real cost and expiry data —
  everything downstream (FEFO sale allocation, COGS, expiry alerts) is built on actual batch cost, not
  today's purchase price.
- Stock overview, low-stock/expiry alerts, and audited manual stock adjustments.

### Point of Sale (POS)
- A fast typeahead medicine search, cart-based sale entry, and **FEFO (First-Expiry-First-Out) batch
  allocation computed server-side** at checkout — the customer is always quoted one consistent price per
  line even when fulfilling it draws from multiple batches behind the scenes.
- Printable receipts; customer credit-limit enforcement on partially-paid sales.

### Returns
- Sales returns (with optional restocking back into the exact original batches) and purchase returns
  to suppliers, each adjusting the relevant running balance.

### Due & Payment Management
- Accounts receivable/payable views and standalone payment recording, plus a full chronological
  **account statement** per customer/supplier with a running balance.

### Expenses & Cash Drawer
- Categorized expense tracking and daily cash-drawer reconciliation (opening/closing cash vs. an
  expected figure computed from the day's cash sales, payments, and expenses).

### Reports & Analytics
- Sales and Purchase reports, Inventory Valuation, and a **Profit & Loss report with COGS computed from
  actual batch cost** — the master requirement this entire data model was built to support — each with
  CSV export.

### Dashboard & Notifications
- Live KPI cards, a sales trend chart, and an in-app notification bell (low stock, expiring/expired
  batches, cash drawers left open) computed fresh on every load.

### Backup & Restore
- One-click and scheduled daily `BACKUP DATABASE`, with retention and a generated restore script for
  SSMS — deliberately **not** an in-app one-click restore, since that would need to take the live
  database the running app depends on offline.

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC / Razor Views, C#, .NET 9 |
| Data access | Entity Framework Core (SQL Server provider) |
| Database | Microsoft SQL Server 2019+ |
| Identity & Auth | ASP.NET Core Identity (`BIGINT` keys) + a custom claims-based permission system |
| Frontend | Bootstrap 5 (vendored locally), vanilla JavaScript (typeahead search, dynamic line-item forms), Chart.js (dashboard chart) |
| Logging | Serilog (console + rolling file) |
| Background jobs | ASP.NET Core `BackgroundService` (scheduled backups) |
| Testing | xUnit (unit tests for all business-logic calculators; integration test scaffold for a live SQL Server) |

## Architecture

A classic layered architecture, dependencies pointing inward only:

```
PharmaDesk.Domain/          Entities only. No dependencies.
PharmaDesk.Application/     Interfaces, permission codes, pure business-logic calculators. Depends on Domain.
PharmaDesk.Infrastructure/   EF Core DbContext, Identity, concrete services. Depends on Application + Domain.
PharmaDesk.Web/              Controllers, Razor views, composition root. Depends on Application + Infrastructure.
PharmaDesk.Tests.Unit/        Fast tests, no external dependencies.
PharmaDesk.Tests.Integration/ Tests requiring a live SQL Server instance.
```

Every risky piece of business math (FEFO allocation, return crediting, cash reconciliation, profit &
loss, statement running balances, backup filename validation) is implemented as a small, pure, directly
unit-tested function in the Application layer — not buried inline in a controller.

See [Documentation/Architecture.md](Documentation/Architecture.md) for the full set of design decisions,
including the multi-tenancy model and why certain things (like restore automation) were deliberately
*not* built.

## Database

The entire schema lives in **one file**: [`Database/PharmaDesk_Complete.sql`](Database/PharmaDesk_Complete.sql).
It is idempotent and version-gated — safe to run against a brand-new server or an existing installation
on any prior version; it only applies whatever hasn't been applied yet. See
[Documentation/DatabaseUpdateGuide.md](Documentation/DatabaseUpdateGuide.md).

## Getting Started

```powershell
# Prerequisites: .NET 9 SDK, a reachable SQL Server 2019+ instance

dotnet build

# Apply the schema
sqlcmd -S <your-server> -E -i Database\PharmaDesk_Complete.sql

# Set the connection string in PharmaDesk.Web/appsettings.Development.json, then:
dotnet run --project PharmaDesk.Web
```

A default admin account is seeded automatically on first run against a fresh database — check the log
file for its one-time temporary password. For full production deployment steps (IIS hosting, HTTPS,
secrets, backup folder permissions), see
[Documentation/DeploymentGuide.md](Documentation/DeploymentGuide.md).

## Documentation

| Document | Covers |
|---|---|
| [ProjectOverview.md](Documentation/ProjectOverview.md) | High-level summary and deployment model |
| [ModuleStatus.md](Documentation/ModuleStatus.md) | Module-by-module build history, decisions, known limitations |
| [Architecture.md](Documentation/Architecture.md) | Layering, multi-tenancy, and key technical design decisions |
| [BusinessRules.md](Documentation/BusinessRules.md) | The business rules behind every module, with rationale |
| [DatabaseDesign.md](Documentation/DatabaseDesign.md) | Every table, by module |
| [DatabaseUpdateGuide.md](Documentation/DatabaseUpdateGuide.md) | How the single-file schema script works |
| [DeploymentGuide.md](Documentation/DeploymentGuide.md) | Local setup and production deployment |
| [ChangeLog.md](Documentation/ChangeLog.md) | Detailed change history, module by module |

## Project Status

All 15 modules from the original development plan — Foundation, Auth, Tenant/Branch, Medicines,
Suppliers/Customers, Purchasing, Inventory, POS, Returns, Due/Payments, Expenses/Cash, Reports,
Dashboard, Backup/Restore, and Security Hardening — are complete. The codebase builds cleanly and its
full unit test suite passes. Behavior against a live SQL Server instance with real data has not yet been
exercised in the development environment this was built in; see `ModuleStatus.md` for exactly what
remains to be verified before go-live.
