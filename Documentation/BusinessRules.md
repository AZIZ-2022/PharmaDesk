# Business Rules

This document accumulates PharmaDesk's business rules as each module implements them.

## Module 1 — Authentication, Users, Roles and Permissions

- **Permissions are a login-time snapshot, not a live check.** A user's effective permission codes (derived
  from their role(s) via RolePermissions) are baked into claims on the auth cookie when they sign in. Changing
  a role's permissions, or a user's role, takes effect the next time that user logs in — not immediately. This
  is a deliberate tradeoff (avoids a database round-trip on every authorized request); if a future module needs
  immediate revocation, that will require re-visiting this design, not silently patching around it.
- **Deactivating a user does not delete them.** `ToggleActive` sets `IsActive` for display purposes and uses
  Identity's lockout mechanism (`LockoutEnd = DateTimeOffset.MaxValue`) to actually block sign-in. Users are
  never hard-deleted once created, since they may already be referenced by transactions in later modules.
  the deactivation endpoint also fully unlocks a reactivated user.
- **The seeded default admin account forces a password change before anything else is reachable.** A fixed,
  shippable default credential is a real risk for commercial software distributed to multiple customers, so
  the generated temporary password is logged once (console/log file) and the account cannot be used for
  anything except changing its own password until that's done.
- **Account lockout:** 5 failed attempts locks the account for 15 minutes (spec's "login rate limiting"
  requirement), independent of the deactivation lockout above — both use the same underlying mechanism but for
  different reasons.

## Module 2 — Tenant, Branch and Organization Management

- **A user's Tenant is mandatory; their Branch is optional.** `BranchId = null` means that user isn't
  restricted to one branch (e.g. Admin, or back-office roles) — it is not an error state or a "needs setup"
  flag. Future modules that scope data by branch should treat `null` as "all branches," not reject it.
- **Tenant data isolation defaults to permissive when there's no ambient tenant, not restrictive.** The EF
  Core query filter on `Branch` (and every tenant-scoped table after it) only filters when
  `ICurrentTenantContext.TenantId` resolves to a value; outside a real request (the identity seeder,
  background jobs) it shows everything. This was a deliberate choice to avoid background work silently seeing
  zero rows — the tradeoff is that any future background job touching tenant-scoped data must set its own
  ambient tenant context explicitly if it needs to be scoped, rather than relying on the filter to protect it.
- **The seeded Tenant is a placeholder, not real business data.** Its name literally says so
  ("Your Pharmacy Name (update in Settings)") so it's never mistaken for a real customer's profile if someone
  forgets to fill it in.
- **At least one Branch must stay active.** `BranchesController.ToggleActive` refuses to deactivate the last
  active branch — there must always be somewhere for a user (or, later, a sale) to be scoped to.

## Module 3 — Medicine Master Data Management

- **Manufacturer (drug company) is distinct from Supplier (wholesaler).** A pharmacy buys from a Supplier
  (Module 4), but the medicine itself was made by a Manufacturer (Module 3). Easy to conflate since both are
  "companies" — kept as separate tables on purpose.
- **Global reference lists vs. tenant-managed lists.** `DosageForms` and `UnitsOfMeasure` are seeded once,
  globally, and not editable through this module's UI — they are a fixed vocabulary, not business data a
  pharmacy customizes. `MedicineCategories` and `Manufacturers` are tenant-scoped and have their own CRUD
  screens, since different pharmacies genuinely organize these differently.
- **Strength is free text, not structured.** Tablet strengths ("500mg"), syrup strengths ("5mg/5ml"), and
  injection strengths ("5mg/ml") don't share one clean structured shape — normalizing this now would be
  premature structure for a field that's purely informational at this stage.
- **A medicine's base unit and pack unit define a conversion, not a stock count.** `PackSize` says how many
  base units (e.g. Tablets) make up one pack unit (e.g. a Strip); Module 3 itself does not track or compute
  any actual stock quantities — that came later, from Module 5 (Purchase, which creates batches) and Module
  6 (which views/adjusts them). Getting this conversion definition right in Module 3 mattered because both
  built directly on top of it.
- **Medicines are deactivated, never hard-deleted**, for the same reason as Users and Branches — later
  modules (Purchase, Sales) will reference them by ID.

## Module 4 — Supplier and Customer Management

- **`OpeningBalance` is captured once; `CurrentBalance` is the live field, but nothing in this module moves
  it except initialization and opening-balance corrections.** `CurrentBalance` starts equal to
  `OpeningBalance` at creation. If `OpeningBalance` is later edited (e.g. correcting a data-entry mistake
  during setup), the edit is applied to `CurrentBalance` as a **delta**, not an overwrite — this matters once
  Module 5/7/9 start posting real transactions against the balance, since an overwrite would silently erase
  that activity. No module before 5 performs any other write to `CurrentBalance`.
- **Most sales don't need a Customer record.** Walk-in/cash sales are the default case in a retail pharmacy;
  requiring a Customer row for every anonymous sale would be the wrong default. Each tenant gets one seeded
  "Walk-in Customer" (`IsWalkInDefault = 1`) for Module 7's POS to attach anonymous sales to.
- **The default Walk-in Customer can never be deactivated.** Same protective pattern as Module 2's
  last-active-branch guard — POS depends on this row always existing and being active.
- **Supplier (wholesaler) and Manufacturer (drug company) are different tables for a reason** — see Module 3's
  note; repeated here because it's easy to conflate the two when actually building a Purchase screen (Module
  5) that needs both.

## Module 5 — Purchase Management

- **A Purchase is create-and-view only — there is no edit screen.** Saving a Purchase moves
  `Supplier.CurrentBalance` and creates `MedicineBatch` rows in the same transaction; editing it afterward
  would require safely reversing both, which is a proper reversal flow (Module 8's job), not a form edit.
  Corrections to an already-saved purchase go through a purchase return, not an edit.
- **Module 5 creates `MedicineBatches`, even though Module 6 is named "Batch-wise Inventory."** Batch
  number, expiry date, and cost are what's physically on the stock arriving from the supplier — they're
  captured at the moment of purchase, not assigned later. Module 6 owns stock-level views, low-stock/expiry
  alerts, and manual stock adjustments on top of the batches Module 5 creates; it does not create them.
  This split was proposed explicitly in Module 5's plan and approved before building it.
- **Quantity is entered in pack units (matching the supplier invoice), converted to base units via
  `Medicine.PackSize` to produce the batch's `QuantityReceived`.** `QuantityRemaining` starts equal to it;
  nothing in this module decrements it — that's Module 7 (POS sales).
- **Selling price is set per-batch, at purchase time, not on the Medicine master record.** This is what
  makes COGS-from-actual-batch-cost (the master spec's requirement) possible — the same medicine can have
  batches with different costs and different margins active simultaneously.
- **`BranchId` on Purchases/MedicineBatches is a required plain column, not tenant-style auto-filtered.**
  See `Architecture.md` — this avoids locking in a branch-isolation policy before Reports/Transfers modules
  decide what they actually need.

## Module 6 — Batch-wise Inventory

- **`QuantityRemaining` can never go negative.** Manual stock adjustments are guarded by
  `StockAdjustmentCalculator.TryApplyAdjustment`, which rejects any adjustment that would push a batch below
  zero. There is no override — if a physical count genuinely needs to go lower than the system thinks,
  that's a sign of a missed transaction worth investigating, not a case to force through.
- **Every adjustment is recorded, never a silent overwrite.** A `StockAdjustment` row (signed delta + reason
  + optional notes) is written in the same transaction as the `MedicineBatch.QuantityRemaining` update, plus
  the standard `AuditLog` entry — inventory corrections need the same traceability as a financial
  transaction, since they directly affect COGS (Module 11).
- **Exhausted batches (QuantityRemaining = 0) are hidden by default, not deleted.** They're excluded from
  the Batches list and Stock Overview unless explicitly shown, but stay in the table for COGS/reporting
  history — same "never hard-delete once it could be referenced" rule as every other module.
- **The expiry-alert threshold (90 days) is a hardcoded constant for now**, not a tenant setting — flagged
  in `ModuleStatus.md` as an easy follow-up (`ApplicationSettings` already exists for it) rather than
  something built speculatively now.

## Module 7 — POS and Sales Management

- **One price per line, even when fulfilling it spans multiple batches.** A customer is quoted the
  soonest-expiring batch's selling price for the full line quantity; the system may actually draw stock
  from a second (differently-costed) batch behind the scenes to fulfil it. The customer never sees "it
  depends which box we grab" — COGS accuracy is preserved separately via `SaleBatchAllocations`, which
  records the real batch(es) and real cost(s) consumed. This was proposed in Module 7's plan and approved
  before building it.
- **FEFO allocation is all-or-nothing.** If a medicine's combined batch stock can't cover a line's
  requested quantity, the entire sale is rejected — no line is partially fulfilled, and no batch is
  partially decremented, before the shortfall is detected.
- **A non-credit-eligible customer (including every Walk-in Customer) can never leave a sale partially
  unpaid.** `AmountPaid` must equal `TotalAmount` unless `Customer.IsCreditEligible` is true. A
  credit-eligible customer with a `CreditLimit` set cannot have a sale push their balance past it.
- **The client-displayed price when a line is added to the cart is a preview only.** The authoritative
  price is always recomputed server-side from the batches at the moment "Complete Sale" is submitted — the
  POST never trusts whatever price value the browser echoes back, precisely because that number is visible
  to, and editable by, anyone with browser dev tools.
- **A Sale is create-and-view only — there is no edit screen**, for the same reason as Purchases
  (Module 5): it has already moved stock and a customer balance. Corrections go through Module 8 (Returns).
- **No thermal-printer / ESC-POS integration.** The receipt (`Sales/Details`) is a plain, browser-printable
  HTML page — real hardware integration needs physical hardware this environment doesn't have, so it's
  explicitly out of scope rather than half-built.

## Module 8 — Sales and Purchase Returns

- **A sale line can be returned once, in full or in part, but never a second time.** Enforced by a unique
  constraint on `SalesReturns.SaleItemId`, not just an app-level check. This is a deliberate v1 scope
  limit — supporting multiple partial returns against the same line would require tracking how much of
  each original batch allocation has already been credited back, which this module doesn't do. Flagged in
  the plan and approved before building.
- **A sales return credits stock back to the exact batches the sale drew from, not wherever FEFO would
  point today.** `SalesReturnAllocator` walks the original `SaleBatchAllocations` in their original order;
  it does not re-run `FefoAllocator`, since the soonest-expiring batch today may not be the one the
  customer's stock actually came from.
- **A return's `IsRestocked` flag decides whether stock changes at all.** A customer returning an expired
  or damaged item should not put that stock back on the shelf — in that case the return is financial-only,
  and no `SalesReturnBatchCredits` rows are created.
- **Balances can go negative, and that's correct, not a bug.** A return reduces what a customer/supplier
  owes; if they'd already paid/been paid in full, the balance goes negative, meaning the pharmacy or
  supplier now owes money back. The system tracks the net ledger — the physical cash/credit-note exchange
  happens outside it, the same way `AmountPaid` already worked for Purchases/Sales.
- **Purchase returns always decrement their batch — there is no restock option**, since the stock is
  physically leaving the premises for the supplier. Multiple partial purchase returns against the same
  batch are fine, unlike sales returns.
- **Both return types are create-and-view only**, consistent with Purchases and Sales — no "undo a
  return."

## Module 9 — Due and Payment Management

- **A payment reduces the overall running balance; it is not matched to a specific sale/purchase
  invoice.** Proposed in the plan and approved: this is simpler and realistic for how a small pharmacy
  actually manages dues ("pay down what you owe in general"), at the cost of invoice-level traceability
  for a payment. If that traceability is ever needed, it's a real redesign, not a quick patch.
  `OpeningBalance`, every sale/purchase's due portion, every return, and now every payment all feed into
  the same running balance — the statement view exists specifically to make that computation visible and
  auditable rather than something you have to trust blindly.
- **No cap against overpayment.** A payment can exceed the current balance, pushing it negative (meaning
  the pharmacy/supplier now owes money back) — the same ledger behavior Module 8 established for returns.
  The form shows the current balance; the person recording the payment decides.
- **The statement's running balance always starts from `OpeningBalance`**, not zero — consistent with
  every balance-related calculation since Module 4, where `OpeningBalance` was established as the
  permanent starting point a running balance is built from.

## Module 10 — Expense and Cash Management

- **Expenses support Edit and Void, breaking from the Purchases/Sales/Returns create-view-only
  precedent.** Flagged and approved: those three modules made editing unsafe because a saved record had
  already moved stock or a party's balance — an expense moves neither, so there's nothing to reverse.
  "Void" (not delete) still applies, consistent with the project-wide "never hard-delete a financial
  record" rule.
- **The cash-drawer expected-closing figure excludes Sales/Purchase Returns.** Module 8 never recorded
  whether a return was settled in cash, store credit, or otherwise, so there's no field to filter a
  return's cash effect by. This is a real accuracy gap for pharmacies with frequent cash refunds, named
  explicitly rather than silently producing a wrong variance. Fixing it means adding a settlement-method
  field to `SalesReturns`/`PurchaseReturns` (a Module 8 schema change), not something Module 10 can patch
  around.
- **Only one cash drawer session can be open per branch at a time.** Enforced at the app level by checking
  for an existing `ClosedAtUtc IS NULL` row for that branch before allowing a new one to open.
- **The expected-closing and variance figures are always recomputed server-side when a session is
  closed**, never trusted from whatever the close form's preview showed — the same pattern
  `SalesController.Complete` established for sale pricing in Module 7.

## Module 11 — Reports and Analytics

- **COGS is calculated from the actual cost of the batches each sale drew from, not today's purchase
  price.** This is why `SaleBatchAllocation.CostPerBaseUnitAtSale` was snapshotted all the way back in
  Module 7 — the Profit & Loss report is the reason that field exists. Gross Profit = Revenue − COGS; Net
  Profit = Gross Profit − Expenses (active expenses dated within the report period).
- **Inventory Valuation is a live snapshot, not a historical "as of date X" figure.** It values whatever
  is currently in `MedicineBatches.QuantityRemaining` at each batch's actual purchase cost, right now. A
  valuation as of a past date would require reconstructing stock levels from transaction history —
  explicitly out of scope for this module, proposed and approved as such in the plan.
- **No invoice-level grand total for quantity columns in the Sales/Purchase reports.** Summing "150
  tablets + 40 ml" across different medicines would be meaningless, so only currency totals (Revenue,
  Total Cost) are grand-totaled; quantities are shown per medicine row only.
- **No charts, scheduled/emailed delivery, or due aging-bucket analysis.** Charts and visual summaries
  belong to Module 12 (Dashboard/Notifications); aging buckets on dues would be a reasonable future
  addition to Module 9's statements but are scope creep for this module.

## Module 12 — Dashboard and Notifications

- **Notifications are live-computed, never persisted, with no read/unread/dismiss state.** Flagged and
  approved in the plan: the conditions they surface (low stock, an expired batch still on the shelf, a
  cash drawer left open) are ongoing facts about current data, not one-time events — dismissing the
  notice wouldn't make the medicine back in stock. The bell always reflects whatever is true right now.
- **Notifications are aggregated per category, not one per affected item.** "5 medicines are low on
  stock," not five separate lines — keeps the bell usable rather than a flood.
- **Every dashboard card and notification item is gated by the same permission that gates its underlying
  screen** (e.g. Receivable/Payable needs `Payments.View`, stock/expiry notices need `Inventory.View`).
  The dashboard is still visible to any authenticated user, same as before — only which cards/notices
  appear changes per viewer.
- **The dashboard defaults to the viewing user's own branch** (from their `BranchId` claim) if they have
  one, otherwise all branches, with an explicit dropdown to override — same UX pattern every other
  branch-aware screen in this app uses.

## Module 13 — Backup and Restore

- **The application creates backups but never restores them.** `BACKUP DATABASE` is safe to run against
  a live database; `RESTORE DATABASE` needs exclusive access to the very database the running app
  depends on, so automating it from inside that same app risks destroying a live database with no way
  back if anything goes wrong. The app generates the exact restore script and the admin runs it in SSMS
  with the application stopped — proposed and approved in the plan, not a shortcut.
- **Backup files are tracked on disk, not mirrored into a database table.** The list screen always
  reflects whatever is actually in the configured folder, so there's no possibility of the database
  recording a backup that doesn't exist, or vice versa.
- **A daily scheduled backup runs automatically**, with retention (oldest backups beyond the configured
  count are deleted) so disk usage doesn't grow unbounded. Manual "Create Backup Now" is still available
  for before a risky operation (e.g. before injecting a new version of `PharmaDesk_Complete.sql`).
- **`BackupSettings:DirectoryPath` must be reachable by both the web application's process and the SQL
  Server service account.** Simplest, and the expected case for a single-pharmacy on-prem install, when
  SQL Server and the web app run on the same machine. A remote SQL Server would need a shared/UNC path
  both sides can use — a real deployment-topology consideration, not automatically satisfied.
- **Every backup filename arriving from a URL (Download, Delete, Restore Instructions) is validated
  before any file-system access** — only a bare `name.bak` with no path separators or `..` is accepted.
  This is the one place in this module a crafted input could otherwise read or delete files outside the
  intended folder.

## Module 14 — Testing, Security Hardening and Deployment

- **A codebase-wide security audit found every controller already correct** — every `[HttpPost]` action
  already had `[ValidateAntiForgeryToken]`, every controller already had `[Authorize]` with
  `[PermissionAuthorize]` on sensitive actions, the login redirect was already guarded against open
  redirect, and no admin-set password was ever logged or stored in plaintext. This is reported as a
  verified finding, not assumed — the alternative to auditing was trusting 13 modules' worth of
  consistency blindly, which isn't the same thing.
- **Three concrete gaps were found and fixed**: no security response headers, no explicit cookie
  `SecurePolicy`, and no rate limiting on the login endpoint beyond Identity's per-account lockout. All
  three are plain ASP.NET Core features, no new package.
- **The login rate limiter is defense-in-depth, not a replacement for account lockout.** Identity's
  lockout (5 failed attempts/15 minutes) is per-account; the rate limiter (10 requests/minute/IP) protects
  the endpoint itself against being flooded regardless of which username each request targets. Verified
  live: 10 attempts succeeded (as login attempts, i.e. returned the normal failed-login page), the 11th
  and 12th were rejected with HTTP 429.
- **Content-Security-Policy was deliberately not added.** Doing it properly means refactoring every
  inline `<script>` block across the app to use nonces — real, view-by-view work that needs browser
  testing this environment can't provide. Named as a follow-up rather than attempted partially or skipped
  silently.
- **This module's security review was a code read-through, not a penetration test.** Nothing in this
  project has been tested against a live, network-reachable instance under adversarial conditions — that
  remains a real gap before shipping to a paying customer, independent of how carefully the code was
  read.

## Pending (to be documented as later modules implement them)

(none remaining from the original master requirements — all 15 modules, 0 through 14, are documented
above)
