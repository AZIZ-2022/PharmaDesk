# Database Update Guide

PharmaDesk's entire database schema lives in **one file**: `Database/PharmaDesk_Complete.sql`. You never need
to run any other SQL file, and you never need to manually create or alter a table.

## How to apply an update

1. Open SQL Server Management Studio (SSMS).
2. Open `Database/PharmaDesk_Complete.sql`.
3. Execute it (F5) against your SQL Server instance.
4. Done — the script detects what's already applied and only runs what's new.

This is safe to do:
- On a fresh server where the `PharmaDesk` database doesn't exist yet (it creates it).
- On an existing `PharmaDesk` database at any earlier version (it skips everything already applied and only
  adds what's missing).
- Repeatedly, by accident — re-running it twice in a row does nothing the second time.

## How this works internally

- `dbo.DatabaseSchemaVersions` records every version that has been applied, with a timestamp.
- Every module's changes live in their own block, guarded by
  `IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = 'x.y.z') BEGIN ... END`.
- Each block runs inside a transaction with `TRY/CATCH`: if anything in that block fails, it rolls back
  entirely rather than leaving the database half-upgraded, and the error is re-thrown so SSMS shows it clearly.
- Blocks are never edited or removed once committed to this guide's history — new modules only append new
  blocks after the last one.

## If a future update is destructive

If a schema change in some module would drop a column, change a data type in a lossy way, or otherwise require
migrating existing data, that will be called out and explained *before* it's added to the script — never
silently included in a routine update.
