# Database Design

Current schema version: **0.14.0** (Modules 0–13). This document is updated after every module with the
tables that module added — it does not restate SQL Server internals, only PharmaDesk's own schema decisions.
Module 11 (Reports and Analytics) added no tables — its four reports (Sales, Purchase, Profit & Loss,
Inventory Valuation) are read-only queries over tables already listed below; schema version 0.12.0 only
seeds the `Reports.View` permission. Module 12 (Dashboard and Notifications) added no tables and no
permissions at all — schema version 0.13.0 is a version-history row only. Module 13 (Backup and Restore)
also added no tables — backup files are tracked on disk, not mirrored into the database — schema version
0.14.0 only seeds the `Backup.View`/`Backup.Manage` permissions.

## dbo.DatabaseSchemaVersions

Tracks which versioned schema blocks in `Database/PharmaDesk_Complete.sql` have been applied to a given
database. Bootstrapped directly (not version-gated itself, since nothing can check a version before this table
exists).

| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY | PK |
| VersionNumber | VARCHAR(20) | Unique, e.g. `0.1.0` |
| ModuleName | NVARCHAR(100) | e.g. `Module 0` |
| Description | NVARCHAR(500) | Human-readable summary of what the version added |
| AppliedAtUtc | DATETIME2(0) | Defaults to `SYSUTCDATETIME()` |
| Success | BIT | Defaults to 1 |

## dbo.ApplicationSettings

Simple key/value store for application-level configuration that isn't a connection string or secret.

| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY | PK |
| SettingKey | NVARCHAR(100) | Unique |
| SettingValue | NVARCHAR(500) | Nullable |
| Description | NVARCHAR(500) | Nullable |
| ModifiedAtUtc | DATETIME2(0) | Defaults to `SYSUTCDATETIME()` |

## dbo.Users / dbo.Roles / dbo.UserRoles / dbo.UserClaims / dbo.UserLogins / dbo.RoleClaims / dbo.UserTokens

ASP.NET Core Identity's standard tables, renamed, with `BIGINT` keys instead of the GUID default. `Users` adds
`FullName`, `IsActive`, `MustChangePassword`, `CreatedAtUtc`, `ModifiedAtUtc`, and, as of Module 2, `TenantId`
(mandatory FK to `Tenants`) and `BranchId` (nullable FK to `Branches` — null means "not restricted to one
branch," e.g. Admin). `Roles` adds `Description`.

## dbo.Permissions

Static catalog of permission codes (e.g. `Users.View`), independent of Identity's own role-claims mechanism so
permissions can be managed and displayed through a UI.

| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY | PK |
| Code | NVARCHAR(100) | Unique, e.g. `Users.Create` |
| Name | NVARCHAR(200) | Display name |
| ModuleName | NVARCHAR(100) | Groups permissions in the UI |
| Description | NVARCHAR(500) | Nullable |

## dbo.RolePermissions

Many-to-many grant table between `Roles` and `Permissions`. A user's effective permissions are the union of
their role(s)' grants, baked into claims on the auth cookie at sign-in time (not re-checked against the
database on every request — see `BusinessRules.md` for the tradeoff this implies).

## dbo.AuditLogs

Append-only log of auth and admin actions (login success/failure/lockout, logout, password change, user/role
created/updated/deactivated). `UserId` is nullable since a failed login attempt may not resolve to a real user.

## dbo.Tenants

The pharmacy business. One row per installed database today (see `Architecture.md`'s multi-tenancy model).

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| Name | NVARCHAR(200) | Required |
| LicenseNumber | NVARCHAR(100) | Nullable — trade license number |
| Phone | NVARCHAR(30) | Nullable |
| Email | NVARCHAR(256) | Nullable |
| Address | NVARCHAR(500) | Nullable |
| IsActive | BIT | Default 1 |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

Seeded with one placeholder row ("Your Pharmacy Name (update in Settings)") so `Users.TenantId` always has
something to point at; edited via the Pharmacy Profile screen.

## dbo.Branches

Physical locations belonging to a Tenant.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| Name | NVARCHAR(200) | Required |
| Address / Phone | NVARCHAR | Nullable |
| IsMainBranch | BIT | Exactly one branch should carry this; app logic prevents deactivating the last active branch |
| IsActive | BIT | Default 1 |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

Queries against `Branches` through `PharmaDeskDbContext` are automatically scoped to the caller's `TenantId`
claim via an EF Core global query filter — see `Architecture.md`. Seeded with one "Main Branch" row tied to
the placeholder Tenant.

## dbo.DosageForms / dbo.UnitsOfMeasure

Global, non-tenant-scoped reference lists seeded once by this script (Tablet/Capsule/Syrup/... for
DosageForms; Piece/Tablet/Strip/Box/... for UnitsOfMeasure). Not editable through a UI in this module — they
are a fixed pharmaceutical vocabulary, unlike the tenant-managed lists below.

| Table | Columns |
|---|---|
| DosageForms | Id (INT IDENTITY, PK), Name (NVARCHAR(50)), IsActive (BIT) |
| UnitsOfMeasure | Id (INT IDENTITY, PK), Name (NVARCHAR(50)), ShortCode (NVARCHAR(10)), IsActive (BIT) |

## dbo.MedicineCategories

Tenant-scoped (each pharmacy manages its own category list — see `BusinessRules.md`).

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| Name | NVARCHAR(200) | Required |
| Description | NVARCHAR(500) | Nullable |
| IsActive | BIT | Default 1 |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

## dbo.Manufacturers

The drug company that makes a medicine — distinct from a Module 4 Supplier (the wholesaler the pharmacy buys
from). Tenant-scoped, same reasoning as MedicineCategories.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| Name | NVARCHAR(200) | Required |
| Country | NVARCHAR(100) | Nullable |
| IsActive | BIT | Default 1 |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

## dbo.Medicines

The sellable product catalog. No stock quantities here — Module 6 (Batch Inventory) owns actual stock
counting; this table only defines what a medicine is and how its units convert.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| BrandName | NVARCHAR(200) | Required, indexed for search |
| GenericName | NVARCHAR(200) | Required, indexed for search |
| ManufacturerId | BIGINT | FK → Manufacturers |
| CategoryId | BIGINT | FK → MedicineCategories |
| DosageFormId | INT | FK → DosageForms |
| Strength | NVARCHAR(50) | Nullable, free text (e.g. "500mg", "5mg/5ml") — not structured |
| BaseUnitId | INT | FK → UnitsOfMeasure — the unit it's sold as (e.g. Tablet) |
| PackUnitId | INT | FK → UnitsOfMeasure — the unit it's purchased as (e.g. Strip) |
| PackSize | DECIMAL(18,2) | How many base units per pack unit (e.g. 10 tablets per strip) |
| ReorderLevel | DECIMAL(18,2) | Threshold, in base units, consumed by later Inventory/Dashboard alerts |
| BarcodeNumber | NVARCHAR(100) | Nullable — no scanning logic yet, just the field |
| RegistrationNumber | NVARCHAR(100) | Nullable — DGDA registration, recordkeeping only |
| IsVatApplicable | BIT | Default 0 |
| VatRate | DECIMAL(18,2) | Nullable — only meaningful when IsVatApplicable is set |
| IsActive | BIT | Default 1 — never hard-deleted once referenced by later modules |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

## dbo.Suppliers

Wholesalers/distributors the pharmacy buys stock from — distinct from `dbo.Manufacturers` (Module 3), which
is the drug company, not who sold it to this pharmacy. Tenant-scoped.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| Name | NVARCHAR(200) | Required, indexed for search |
| ContactPerson | NVARCHAR(200) | Nullable |
| Phone / Email / Address | NVARCHAR | Nullable |
| OpeningBalance | DECIMAL(18,2) | Captured once at creation |
| CurrentBalance | DECIMAL(18,2) | Initialized from OpeningBalance; Module 5 (Purchase) is what moves it afterward |
| IsActive | BIT | Default 1 — never hard-deleted |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

## dbo.Customers

Most sales are anonymous walk-in/cash and never need a row here. Tenant-scoped.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| Name | NVARCHAR(200) | Required, indexed for search |
| Phone / Email / Address | NVARCHAR | Nullable |
| IsCreditEligible | BIT | Default 0 |
| CreditLimit | DECIMAL(18,2) | Nullable, only meaningful when IsCreditEligible is set |
| OpeningBalance | DECIMAL(18,2) | Captured once at creation |
| CurrentBalance | DECIMAL(18,2) | Initialized from OpeningBalance; Module 7/9 (Sales/Payments) move it afterward |
| IsWalkInDefault | BIT | Marks the one seeded fallback row per tenant Module 7's POS attaches anonymous sales to; the app refuses to deactivate it |
| IsActive | BIT | Default 1 — never hard-deleted |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

Seeded with one "Walk-in Customer" row (`IsWalkInDefault = 1`) per existing tenant.

## dbo.Purchases

The financial record of buying stock from a Supplier. Create-and-view only through the app — no edit
screen; corrections flow through purchase returns (Module 8), not an edit, since a saved purchase has
already moved a Supplier balance and created batches.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| BranchId | BIGINT | FK → Branches — required; not auto-filtered by an EF query filter (see `Architecture.md`) |
| SupplierId | BIGINT | FK → Suppliers, indexed |
| PurchaseDate | DATETIME2(0) | Indexed |
| InvoiceNumber | NVARCHAR(100) | Nullable — the supplier's own reference |
| SubTotal / DiscountAmount / TaxAmount / TotalAmount / AmountPaid | DECIMAL(18,2) | TotalAmount = SubTotal − Discount + Tax |
| CreatedAtUtc | DATETIME2(0) | |

## dbo.PurchaseItems

One row per line item on a Purchase — one medicine's batch as invoiced. Quantity is in the medicine's pack
unit, matching the supplier invoice.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| PurchaseId | BIGINT | FK → Purchases, cascades on delete, indexed |
| MedicineId | BIGINT | FK → Medicines |
| BatchNumber | NVARCHAR(100) | As printed on the stock received |
| ExpiryDate | DATE | |
| QuantityPacks | DECIMAL(18,2) | In `Medicine.PackUnitId`'s unit |
| PurchasePricePerPack | DECIMAL(18,2) | |
| SellingPricePerBaseUnit | DECIMAL(18,2) | Decided at purchase time; becomes the resulting batch's selling price |
| LineTotal | DECIMAL(18,2) | QuantityPacks × PurchasePricePerPack |

## dbo.MedicineBatches

The stock-tracking record created by a purchase. `QuantityRemaining` starts equal to `QuantityReceived` and
is decremented by Module 7 (POS) sales. Module 6 owns reporting/alerts/adjustments on top of this table —
see `Architecture.md` and `BusinessRules.md` for why Module 5 (not 6) creates the rows.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| BranchId | BIGINT | FK → Branches |
| MedicineId | BIGINT | FK → Medicines, indexed |
| SupplierId | BIGINT | FK → Suppliers |
| PurchaseId / PurchaseItemId | BIGINT | FK → Purchases / PurchaseItems — traceability back to the purchase |
| BatchNumber | NVARCHAR(100) | |
| ExpiryDate | DATE | Indexed — Module 6's expiry alerts will query this |
| QuantityReceived | DECIMAL(18,2) | QuantityPacks × Medicine.PackSize at purchase time |
| QuantityRemaining | DECIMAL(18,2) | Starts equal to QuantityReceived |
| PurchaseCostPerBaseUnit | DECIMAL(18,2) | PurchasePricePerPack ÷ PackSize — the actual cost basis for COGS (Module 11) |
| SellingPricePerBaseUnit | DECIMAL(18,2) | Can differ batch to batch for the same medicine |
| CreatedAtUtc | DATETIME2(0) | |

## dbo.StockAdjustments

An audit trail for manual corrections to `MedicineBatches.QuantityRemaining` (damage, expired write-off,
physical count correction). Who made the adjustment is already captured by the app's `AuditLogs` entry, not
duplicated here.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | Indexed |
| BranchId | BIGINT | Copied from the batch's branch at adjustment time |
| MedicineBatchId | BIGINT | FK → MedicineBatches, indexed |
| QuantityDelta | DECIMAL(18,2) | Signed — positive or negative |
| Reason | NVARCHAR(100) | Damage / Expired / Physical Count Correction / Other |
| Notes | NVARCHAR(500) | Nullable |
| AdjustedAtUtc | DATETIME2(0) | |

Stock Overview, the batch list, and expiry alerts (Module 6's other screens) are read-only views over
`Medicines` and `MedicineBatches` — no new tables were needed for them.

## dbo.Sales

The financial record of selling to a Customer. Create-and-view only through the app — no edit screen, same
reasoning as `dbo.Purchases`.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| BranchId | BIGINT | FK → Branches |
| CustomerId | BIGINT | FK → Customers, indexed — defaults to the tenant's Walk-in Customer |
| SaleDate | DATETIME2(0) | Indexed |
| SubTotal / DiscountAmount / TaxAmount / TotalAmount / AmountPaid | DECIMAL(18,2) | TotalAmount = SubTotal − Discount + Tax |
| PaymentMethod | NVARCHAR(50) | Nullable — Cash / Card / Mobile Banking |
| CreatedAtUtc | DATETIME2(0) | |

## dbo.SaleItems

One row per medicine line. Quantity is in **base units** (individual tablets/pieces — unlike
`PurchaseItems`, which is in pack units), since that's what a customer actually buys.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| SaleId | BIGINT | FK → Sales, cascades on delete, indexed |
| MedicineId | BIGINT | FK → Medicines |
| QuantityBaseUnits | DECIMAL(18,2) | |
| UnitPrice | DECIMAL(18,2) | Quoted from the soonest-expiring batch with stock at sale time; locked for the whole line |
| LineTotal | DECIMAL(18,2) | QuantityBaseUnits × UnitPrice |

## dbo.SaleBatchAllocations

Exactly which `MedicineBatches` were FEFO-consumed to fulfil a `SaleItem`, and at what actual cost — the
COGS traceability record Module 11 will read from.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| SaleItemId | BIGINT | FK → SaleItems, cascades on delete, indexed |
| MedicineBatchId | BIGINT | FK → MedicineBatches, indexed |
| QuantityAllocated | DECIMAL(18,2) | |
| CostPerBaseUnitAtSale | DECIMAL(18,2) | Snapshot of the batch's cost at the moment of sale |

## dbo.SalesReturns

A customer returning medicine from a past sale line. The unique index on `SaleItemId` enforces this
module's v1 scope limit — a given sale line can only be returned once, for up to its full quantity.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | Indexed |
| BranchId | BIGINT | Copied from the original Sale |
| SaleId | BIGINT | FK → Sales |
| SaleItemId | BIGINT | FK → SaleItems, **unique** |
| CustomerId | BIGINT | FK → Customers |
| QuantityReturned | DECIMAL(18,2) | ≤ the original line's QuantityBaseUnits |
| UnitPriceAtSale | DECIMAL(18,2) | Snapshot from the original SaleItem |
| ReturnAmount | DECIMAL(18,2) | QuantityReturned × UnitPriceAtSale |
| IsRestocked | BIT | Default 1 — if false, no stock change, financial-only |
| Reason | NVARCHAR(100) | Expired / Damaged / Customer Changed Mind / Other |
| Notes | NVARCHAR(500) | Nullable |
| ReturnedAtUtc | DATETIME2(0) | |

## dbo.SalesReturnBatchCredits

Which `MedicineBatches` were credited back when `IsRestocked = 1`, mirroring the original
`SaleBatchAllocations` rather than re-deriving FEFO. Empty for non-restocked returns.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| SalesReturnId | BIGINT | FK → SalesReturns, cascades on delete, indexed |
| MedicineBatchId | BIGINT | FK → MedicineBatches, indexed |
| QuantityCredited | DECIMAL(18,2) | |

## dbo.PurchaseReturns

The pharmacy sending stock back to a Supplier. Always decrements its batch — there is no restock option,
since the stock physically leaves. Multiple partial returns against the same batch are allowed.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | Indexed |
| BranchId | BIGINT | Copied from the batch |
| PurchaseId | BIGINT | FK → Purchases |
| MedicineBatchId | BIGINT | FK → MedicineBatches, indexed |
| SupplierId | BIGINT | FK → Suppliers |
| QuantityReturned | DECIMAL(18,2) | ≤ the batch's current QuantityRemaining at the time |
| CostPerBaseUnitAtPurchase | DECIMAL(18,2) | Snapshot from the batch |
| ReturnAmount | DECIMAL(18,2) | QuantityReturned × CostPerBaseUnitAtPurchase |
| Reason | NVARCHAR(100) | Expired / Damaged / Wrong Item / Other |
| Notes | NVARCHAR(500) | Nullable |
| ReturnedAtUtc | DATETIME2(0) | |

## dbo.CustomerPayments / dbo.SupplierPayments

Standalone payments against the running balance — not matched to a specific Sale/Purchase invoice (see
`BusinessRules.md`). Each reduces its party's `CurrentBalance`.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | Indexed |
| BranchId | BIGINT | Where the payment was recorded |
| CustomerId / SupplierId | BIGINT | FK, indexed |
| Amount | DECIMAL(18,2) | No cap against overpayment |
| PaymentMethod | NVARCHAR(50) | Nullable — Cash / Card / Mobile Banking |
| PaymentDate | DATETIME2(0) | |
| Notes | NVARCHAR(500) | Nullable |
| CreatedAtUtc | DATETIME2(0) | |

The Accounts Receivable/Payable and statement views (Module 9's other screens) are read-only, computed
from `Sales`/`SalesReturns`/`CustomerPayments` (or the Purchase equivalents) — no additional tables needed.

## dbo.ExpenseCategories

Tenant-scoped (each pharmacy manages its own list — Rent, Utilities, Salary, etc.), same pattern as
`MedicineCategories`.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | FK → Tenants, indexed |
| Name | NVARCHAR(200) | Required |
| Description | NVARCHAR(500) | Nullable |
| IsActive | BIT | Default 1 |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

## dbo.Expenses

Unlike `Purchases`/`Sales`/`SalesReturns`/`PurchaseReturns`, these support Edit and Void (soft-deactivate
via `IsActive`) rather than being create-and-view only — an expense doesn't move stock or any party's
balance, so there's nothing else to reverse.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | Indexed |
| BranchId | BIGINT | FK → Branches, indexed |
| ExpenseCategoryId | BIGINT | FK → ExpenseCategories |
| Amount | DECIMAL(18,2) | |
| PaymentMethod | NVARCHAR(50) | Nullable — Cash / Bank / Mobile Banking |
| ExpenseDate | DATETIME2(0) | Indexed |
| Notes | NVARCHAR(500) | Nullable |
| IsActive | BIT | Default 1 — "Void" sets this false; never hard-deleted |
| CreatedAtUtc / ModifiedAtUtc | DATETIME2(0) | |

## dbo.CashDrawerSessions

One open session per branch at a time (enforced at the app level — "open" means `ClosedAtUtc IS NULL`,
not a unique column). `ExpectedClosingCashAmount` is computed at close time from cash `Sales`/
`CustomerPayments`/`Expenses`/`SupplierPayments` for that branch and date; it deliberately excludes
`SalesReturns`/`PurchaseReturns`, which never recorded a cash/non-cash settlement marker — see
`BusinessRules.md`.

| Column | Type | Notes |
|---|---|---|
| Id | BIGINT IDENTITY | PK |
| TenantId | BIGINT | Indexed |
| BranchId | BIGINT | FK → Branches, indexed |
| SessionDate | DATE | |
| OpeningCashAmount | DECIMAL(18,2) | Entered when the session opens |
| ClosingCashAmount | DECIMAL(18,2) | Nullable until closed — the physically-counted amount |
| ExpectedClosingCashAmount | DECIMAL(18,2) | Nullable until closed — computed, never trusted from the client |
| Variance | DECIMAL(18,2) | Nullable until closed — ClosingCashAmount − ExpectedClosingCashAmount |
| Notes | NVARCHAR(500) | Nullable |
| OpenedAtUtc | DATETIME2(0) | |
| ClosedAtUtc | DATETIME2(0) | Nullable — null means still open |

No other business tables exist yet — Reports/Analytics, etc. are introduced by their respective modules
(see the proposed module order in the project's master requirements).
