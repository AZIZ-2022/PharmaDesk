# PharmaDesk — Project Overview

PharmaDesk is a commercial pharmacy management system being built for sale to pharmacies in Bangladesh, starting with one medium-sized pharmacy customer. It covers medicine and batch-wise inventory management, purchasing, POS sales, returns, supplier/customer dues, expenses, reporting, and multi-branch operation for a single pharmacy business, with the schema designed to support a future multi-tenant SaaS deployment without a rewrite.

## Stack

- ASP.NET Core MVC/Razor, C#, currently targeting **.NET 9** (planned upgrade to .NET 10 LTS once that SDK is installed on the development machine)
- Entity Framework Core (SQL Server provider) for data access
- Microsoft SQL Server 2019+ as the database engine
- Bootstrap 5, vendored locally (no CDN dependency, matching the project's on-prem/offline deployment model)
- Serilog for structured logging
- xUnit for unit and integration testing

## Deployment model

One SQL Server database per pharmacy customer (on-prem, no internet dependency for core operation or backup). Every business table carries `TenantId`/`BranchId` columns from the start so the same schema can later support a shared-database SaaS deployment, but today each installation has exactly one Tenant with one or more Branches.

## Where things stand

See [ModuleStatus.md](ModuleStatus.md) for the live module-by-module status. Development proceeds one module at a time with explicit approval required before and after each module, per the project's governing workflow.
