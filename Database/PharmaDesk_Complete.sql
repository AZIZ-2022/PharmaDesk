/* ============================================================================
   PharmaDesk_Complete.sql

   SINGLE AUTHORITATIVE DATABASE SCRIPT FOR PHARMADESK.

   This file is the only database artifact you need to manage. Every
   completed module appends a new version-gated block to this same file;
   nothing earlier is ever removed or rewritten.

   Safe to run against:
     A) A server where the PharmaDesk database does not exist yet.
     B) An existing PharmaDesk database already at some prior version.

   How it works:
     - The database is created if missing.
     - dbo.DatabaseSchemaVersions tracks which versions have been applied.
     - Each module's changes live in one IF NOT EXISTS (... version ...) block,
       wrapped in a transaction with TRY/CATCH, so re-running this file is a
       safe no-op for versions already applied.
     - Re-run this entire file any time you receive an updated copy. Just
       open it in SSMS and execute (F5).

   Current schema version: 0.15.0 (Module 14 - Testing, Security Hardening and Deployment)
   ============================================================================ */

SET NOCOUNT ON;
GO

-- ============================================================================
-- STEP 1: Create the database if it does not exist
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'PharmaDesk')
BEGIN
    PRINT 'Creating database [PharmaDesk]...';
    EXEC ('CREATE DATABASE [PharmaDesk]');
END
ELSE
BEGIN
    PRINT 'Database [PharmaDesk] already exists.';
END
GO

USE [PharmaDesk];
GO

-- ============================================================================
-- STEP 2: Bootstrap dbo.DatabaseSchemaVersions
-- This table must exist before any later block can check "has version X
-- been applied?", so it is created directly (guarded by OBJECT_ID) rather
-- than through the version-gate pattern used by every module after it.
-- ============================================================================
IF OBJECT_ID(N'dbo.DatabaseSchemaVersions', N'U') IS NULL
BEGIN
    PRINT 'Creating dbo.DatabaseSchemaVersions...';

    CREATE TABLE dbo.DatabaseSchemaVersions
    (
        Id              INT IDENTITY(1,1)   NOT NULL,
        VersionNumber   VARCHAR(20)         NOT NULL,
        ModuleName      NVARCHAR(100)       NOT NULL,
        Description     NVARCHAR(500)       NULL,
        AppliedAtUtc    DATETIME2(0)        NOT NULL CONSTRAINT DF_DatabaseSchemaVersions_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
        Success         BIT                 NOT NULL CONSTRAINT DF_DatabaseSchemaVersions_Success DEFAULT (1),
        CONSTRAINT PK_DatabaseSchemaVersions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_DatabaseSchemaVersions_VersionNumber UNIQUE (VersionNumber)
    );
END
GO

-- ============================================================================
-- MODULE 0: Project Foundation and Architecture  (schema version 0.1.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.1.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.1.0 (Module 0: Project Foundation and Architecture)...';

        CREATE TABLE dbo.ApplicationSettings
        (
            Id              INT IDENTITY(1,1)   NOT NULL,
            SettingKey      NVARCHAR(100)       NOT NULL,
            SettingValue    NVARCHAR(500)       NULL,
            Description     NVARCHAR(500)       NULL,
            ModifiedAtUtc   DATETIME2(0)        NOT NULL CONSTRAINT DF_ApplicationSettings_ModifiedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_ApplicationSettings PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT UQ_ApplicationSettings_SettingKey UNIQUE (SettingKey)
        );

        INSERT INTO dbo.ApplicationSettings (SettingKey, SettingValue, Description)
        VALUES
            (N'ApplicationName', N'PharmaDesk', N'Display name of the application'),
            (N'SchemaBootstrapCompletedUtc', CONVERT(NVARCHAR(50), SYSUTCDATETIME(), 126), N'Timestamp when the Module 0 schema was first applied to this database');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.1.0', N'Module 0', N'Project Foundation and Architecture: DatabaseSchemaVersions + ApplicationSettings bootstrap.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.1.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.1.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.1.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 1: Authentication, Users, Roles and Permissions  (schema version 0.2.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.2.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.2.0 (Module 1: Authentication, Users, Roles and Permissions)...';

        -- Users (ASP.NET Core Identity, BIGINT keys, extended with PharmaDesk-specific fields)
        CREATE TABLE dbo.Users
        (
            Id                      BIGINT IDENTITY(1,1)   NOT NULL,
            UserName                NVARCHAR(256)           NULL,
            NormalizedUserName      NVARCHAR(256)           NULL,
            Email                   NVARCHAR(256)           NULL,
            NormalizedEmail         NVARCHAR(256)           NULL,
            EmailConfirmed          BIT                     NOT NULL CONSTRAINT DF_Users_EmailConfirmed DEFAULT (0),
            PasswordHash            NVARCHAR(MAX)           NULL,
            SecurityStamp           NVARCHAR(MAX)           NULL,
            ConcurrencyStamp        NVARCHAR(MAX)           NULL,
            PhoneNumber             NVARCHAR(MAX)           NULL,
            PhoneNumberConfirmed    BIT                     NOT NULL CONSTRAINT DF_Users_PhoneNumberConfirmed DEFAULT (0),
            TwoFactorEnabled        BIT                     NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT (0),
            LockoutEnd              DATETIMEOFFSET(7)       NULL,
            LockoutEnabled          BIT                     NOT NULL CONSTRAINT DF_Users_LockoutEnabled DEFAULT (1),
            AccessFailedCount       INT                     NOT NULL CONSTRAINT DF_Users_AccessFailedCount DEFAULT (0),
            FullName                NVARCHAR(200)           NOT NULL,
            IsActive                BIT                     NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
            MustChangePassword      BIT                     NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT (1),
            CreatedAtUtc            DATETIME2(0)            NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc           DATETIME2(0)            NULL,
            CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id)
        );

        CREATE UNIQUE NONCLUSTERED INDEX UserNameIndex ON dbo.Users (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
        CREATE NONCLUSTERED INDEX EmailIndex ON dbo.Users (NormalizedEmail);

        -- Roles
        CREATE TABLE dbo.Roles
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            Name                NVARCHAR(256)           NULL,
            NormalizedName      NVARCHAR(256)           NULL,
            ConcurrencyStamp    NVARCHAR(MAX)           NULL,
            Description         NVARCHAR(500)           NULL,
            CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id)
        );

        CREATE UNIQUE NONCLUSTERED INDEX RoleNameIndex ON dbo.Roles (NormalizedName) WHERE NormalizedName IS NOT NULL;

        -- UserRoles
        CREATE TABLE dbo.UserRoles
        (
            UserId  BIGINT NOT NULL,
            RoleId  BIGINT NOT NULL,
            CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
            CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
            CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
        );

        CREATE NONCLUSTERED INDEX IX_UserRoles_RoleId ON dbo.UserRoles (RoleId);

        -- UserClaims
        CREATE TABLE dbo.UserClaims
        (
            Id          INT IDENTITY(1,1)   NOT NULL,
            UserId      BIGINT              NOT NULL,
            ClaimType   NVARCHAR(MAX)       NULL,
            ClaimValue  NVARCHAR(MAX)       NULL,
            CONSTRAINT PK_UserClaims PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_UserClaims_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
        );

        CREATE NONCLUSTERED INDEX IX_UserClaims_UserId ON dbo.UserClaims (UserId);

        -- UserLogins
        CREATE TABLE dbo.UserLogins
        (
            LoginProvider           NVARCHAR(450)   NOT NULL,
            ProviderKey             NVARCHAR(450)   NOT NULL,
            ProviderDisplayName     NVARCHAR(MAX)   NULL,
            UserId                  BIGINT          NOT NULL,
            CONSTRAINT PK_UserLogins PRIMARY KEY CLUSTERED (LoginProvider, ProviderKey),
            CONSTRAINT FK_UserLogins_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
        );

        CREATE NONCLUSTERED INDEX IX_UserLogins_UserId ON dbo.UserLogins (UserId);

        -- RoleClaims
        CREATE TABLE dbo.RoleClaims
        (
            Id          INT IDENTITY(1,1)   NOT NULL,
            RoleId      BIGINT              NOT NULL,
            ClaimType   NVARCHAR(MAX)       NULL,
            ClaimValue  NVARCHAR(MAX)       NULL,
            CONSTRAINT PK_RoleClaims PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_RoleClaims_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
        );

        CREATE NONCLUSTERED INDEX IX_RoleClaims_RoleId ON dbo.RoleClaims (RoleId);

        -- UserTokens
        CREATE TABLE dbo.UserTokens
        (
            UserId          BIGINT          NOT NULL,
            LoginProvider   NVARCHAR(450)   NOT NULL,
            Name            NVARCHAR(450)   NOT NULL,
            Value           NVARCHAR(MAX)   NULL,
            CONSTRAINT PK_UserTokens PRIMARY KEY CLUSTERED (UserId, LoginProvider, Name),
            CONSTRAINT FK_UserTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
        );

        -- Permissions catalog
        CREATE TABLE dbo.Permissions
        (
            Id          INT IDENTITY(1,1)       NOT NULL,
            Code        NVARCHAR(100)           NOT NULL,
            Name        NVARCHAR(200)           NOT NULL,
            ModuleName  NVARCHAR(100)           NOT NULL,
            Description NVARCHAR(500)           NULL,
            CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT UQ_Permissions_Code UNIQUE (Code)
        );

        -- RolePermissions
        CREATE TABLE dbo.RolePermissions
        (
            RoleId          BIGINT  NOT NULL,
            PermissionId    INT     NOT NULL,
            CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (RoleId, PermissionId),
            CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE,
            CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id) ON DELETE CASCADE
        );

        -- AuditLogs
        CREATE TABLE dbo.AuditLogs
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            UserId          BIGINT                  NULL,
            Action          NVARCHAR(100)           NOT NULL,
            EntityName      NVARCHAR(100)           NULL,
            EntityId        NVARCHAR(100)           NULL,
            Details         NVARCHAR(MAX)           NULL,
            IpAddress       NVARCHAR(45)            NULL,
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_AuditLogs_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id)
        );

        CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON dbo.AuditLogs (UserId);
        CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAtUtc ON dbo.AuditLogs (CreatedAtUtc);

        -- Seed baseline roles
        INSERT INTO dbo.Roles (Name, NormalizedName, ConcurrencyStamp, Description)
        VALUES
            (N'Admin', N'ADMIN', CONVERT(NVARCHAR(36), NEWID()), N'Full system access'),
            (N'Pharmacist', N'PHARMACIST', CONVERT(NVARCHAR(36), NEWID()), N'Pharmacy operations staff'),
            (N'Cashier', N'CASHIER', CONVERT(NVARCHAR(36), NEWID()), N'Point-of-sale staff');

        -- Seed baseline permissions (only for what exists so far: user & role management)
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Users.View', N'View users', N'Users', N'View the list of system users'),
            (N'Users.Create', N'Create users', N'Users', N'Create new system users'),
            (N'Users.Edit', N'Edit users', N'Users', N'Edit existing system users'),
            (N'Users.Deactivate', N'Deactivate users', N'Users', N'Deactivate/reactivate system users'),
            (N'Roles.View', N'View roles', N'Roles', N'View roles and their permissions'),
            (N'Roles.Manage', N'Manage roles', N'Roles', N'Create/edit roles and assign permissions');

        -- Grant all current permissions to Admin only; Pharmacist/Cashier get none yet
        -- since no operational permissions exist until later modules.
        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN';

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.2.0', N'Module 1', N'Authentication, Users, Roles and Permissions: Identity tables (Users/Roles/UserRoles/UserClaims/UserLogins/RoleClaims/UserTokens), Permissions catalog, RolePermissions, AuditLogs, and baseline Admin/Pharmacist/Cashier roles. The first admin user account itself is seeded by the application at startup, not by this script, because its password hash can only be produced correctly by Identity''s own hasher.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.2.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.2.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.2.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 2: Tenant, Branch and Organization Management  (schema version 0.3.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.3.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.3.0 (Module 2: Tenant, Branch and Organization Management)...';

        -- Tenants (the pharmacy business). One installation = one Tenant row today;
        -- the schema still carries TenantId everywhere so a future shared-database
        -- SaaS deployment does not require a rewrite.
        CREATE TABLE dbo.Tenants
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            Name            NVARCHAR(200)           NOT NULL,
            LicenseNumber   NVARCHAR(100)           NULL,
            Phone           NVARCHAR(30)            NULL,
            Email           NVARCHAR(256)           NULL,
            Address         NVARCHAR(500)           NULL,
            IsActive        BIT                     NOT NULL CONSTRAINT DF_Tenants_IsActive DEFAULT (1),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_Tenants_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc   DATETIME2(0)            NULL,
            CONSTRAINT PK_Tenants PRIMARY KEY CLUSTERED (Id)
        );

        -- Branches (physical locations belonging to a Tenant)
        CREATE TABLE dbo.Branches
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            Name            NVARCHAR(200)           NOT NULL,
            Address         NVARCHAR(500)           NULL,
            Phone           NVARCHAR(30)            NULL,
            IsMainBranch    BIT                     NOT NULL CONSTRAINT DF_Branches_IsMainBranch DEFAULT (0),
            IsActive        BIT                     NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT (1),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_Branches_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc   DATETIME2(0)            NULL,
            CONSTRAINT PK_Branches PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Branches_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Branches_TenantId ON dbo.Branches (TenantId);

        -- Seed the single placeholder tenant and its main branch. "Placeholder" is
        -- deliberate: this is not real business data, and the dashboard/profile
        -- screen makes clear it needs to be filled in.
        INSERT INTO dbo.Tenants (Name, IsActive)
        VALUES (N'Your Pharmacy Name (update in Settings)', 1);

        DECLARE @TenantId BIGINT = SCOPE_IDENTITY();

        INSERT INTO dbo.Branches (TenantId, Name, IsMainBranch, IsActive)
        VALUES (@TenantId, N'Main Branch', 1, 1);

        -- Give every existing user (if any; the admin account may already exist
        -- from a prior app run) a home Tenant, then make the column mandatory.
        ALTER TABLE dbo.Users ADD TenantId BIGINT NULL;
        ALTER TABLE dbo.Users ADD BranchId BIGINT NULL;

        EXEC sp_executesql
        N'UPDATE dbo.Users SET TenantId = @TenantId WHERE TenantId IS NULL;',
        N'@TenantId BIGINT',
        @TenantId = @TenantId;

        ALTER TABLE dbo.Users ALTER COLUMN TenantId BIGINT NOT NULL;

        ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id);
        ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id);

        CREATE NONCLUSTERED INDEX IX_Users_TenantId ON dbo.Users (TenantId);
        CREATE NONCLUSTERED INDEX IX_Users_BranchId ON dbo.Users (BranchId);

        -- Seed this module's permissions and grant them to Admin only (filtered by
        -- the new codes specifically - Admin already has Module 1's permissions,
        -- and re-granting those here would violate the RolePermissions primary key).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Tenant.Manage', N'Manage pharmacy profile', N'Organization', N'Edit the pharmacy business profile'),
            (N'Branches.View', N'View branches', N'Organization', N'View the list of branches'),
            (N'Branches.Manage', N'Manage branches', N'Organization', N'Create/edit branches');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Tenant.Manage', N'Branches.View', N'Branches.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.3.0', N'Module 2', N'Tenant, Branch and Organization Management: Tenants/Branches tables, Users.TenantId/BranchId backfill, Organization permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.3.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.3.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.3.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 3: Medicine Master Data Management  (schema version 0.4.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.4.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.4.0 (Module 3: Medicine Master Data Management)...';

        -- DosageForms and UnitsOfMeasure are global reference lists (a fixed
        -- pharmaceutical vocabulary), not tenant-scoped like everything else in
        -- this module - see BusinessRules.md.
        CREATE TABLE dbo.DosageForms
        (
            Id          INT IDENTITY(1,1)   NOT NULL,
            Name        NVARCHAR(50)        NOT NULL,
            IsActive    BIT                 NOT NULL CONSTRAINT DF_DosageForms_IsActive DEFAULT (1),
            CONSTRAINT PK_DosageForms PRIMARY KEY CLUSTERED (Id)
        );

        CREATE TABLE dbo.UnitsOfMeasure
        (
            Id          INT IDENTITY(1,1)   NOT NULL,
            Name        NVARCHAR(50)        NOT NULL,
            ShortCode   NVARCHAR(10)        NOT NULL,
            IsActive    BIT                 NOT NULL CONSTRAINT DF_UnitsOfMeasure_IsActive DEFAULT (1),
            CONSTRAINT PK_UnitsOfMeasure PRIMARY KEY CLUSTERED (Id)
        );

        -- MedicineCategories and Manufacturers are tenant-scoped: each pharmacy
        -- manages its own list (one calls it "Pain Relief", another "Analgesics").
        CREATE TABLE dbo.MedicineCategories
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            Name            NVARCHAR(200)           NOT NULL,
            Description     NVARCHAR(500)           NULL,
            IsActive        BIT                     NOT NULL CONSTRAINT DF_MedicineCategories_IsActive DEFAULT (1),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_MedicineCategories_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc   DATETIME2(0)            NULL,
            CONSTRAINT PK_MedicineCategories PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_MedicineCategories_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
        );

        CREATE NONCLUSTERED INDEX IX_MedicineCategories_TenantId ON dbo.MedicineCategories (TenantId);

        CREATE TABLE dbo.Manufacturers
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            Name            NVARCHAR(200)           NOT NULL,
            Country         NVARCHAR(100)           NULL,
            IsActive        BIT                     NOT NULL CONSTRAINT DF_Manufacturers_IsActive DEFAULT (1),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_Manufacturers_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc   DATETIME2(0)            NULL,
            CONSTRAINT PK_Manufacturers PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Manufacturers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Manufacturers_TenantId ON dbo.Manufacturers (TenantId);

        -- Medicines: the sellable product catalog. Strength is free text (tablet
        -- "500mg" vs. syrup "5mg/5ml" vs. injection "5mg/ml" don't normalize
        -- cleanly into one structured shape). BaseUnit is what it's sold as
        -- (e.g. Tablet); PackUnit is what it's purchased as (e.g. Strip); PackSize
        -- is how many base units make up one pack unit. Actual stock quantities
        -- are tracked starting Module 6 - this table only defines the conversion.
        CREATE TABLE dbo.Medicines
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId            BIGINT                  NOT NULL,
            BrandName           NVARCHAR(200)           NOT NULL,
            GenericName         NVARCHAR(200)           NOT NULL,
            ManufacturerId      BIGINT                  NOT NULL,
            CategoryId          BIGINT                  NOT NULL,
            DosageFormId        INT                     NOT NULL,
            Strength            NVARCHAR(50)            NULL,
            BaseUnitId          INT                     NOT NULL,
            PackUnitId          INT                     NOT NULL,
            PackSize            DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Medicines_PackSize DEFAULT (1),
            ReorderLevel        DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Medicines_ReorderLevel DEFAULT (0),
            BarcodeNumber       NVARCHAR(100)           NULL,
            RegistrationNumber  NVARCHAR(100)           NULL,
            IsVatApplicable     BIT                     NOT NULL CONSTRAINT DF_Medicines_IsVatApplicable DEFAULT (0),
            VatRate             DECIMAL(18,2)           NULL,
            IsActive            BIT                     NOT NULL CONSTRAINT DF_Medicines_IsActive DEFAULT (1),
            CreatedAtUtc        DATETIME2(0)            NOT NULL CONSTRAINT DF_Medicines_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc       DATETIME2(0)            NULL,
            CONSTRAINT PK_Medicines PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Medicines_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
            CONSTRAINT FK_Medicines_Manufacturers FOREIGN KEY (ManufacturerId) REFERENCES dbo.Manufacturers (Id),
            CONSTRAINT FK_Medicines_MedicineCategories FOREIGN KEY (CategoryId) REFERENCES dbo.MedicineCategories (Id),
            CONSTRAINT FK_Medicines_DosageForms FOREIGN KEY (DosageFormId) REFERENCES dbo.DosageForms (Id),
            CONSTRAINT FK_Medicines_BaseUnit FOREIGN KEY (BaseUnitId) REFERENCES dbo.UnitsOfMeasure (Id),
            CONSTRAINT FK_Medicines_PackUnit FOREIGN KEY (PackUnitId) REFERENCES dbo.UnitsOfMeasure (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Medicines_TenantId ON dbo.Medicines (TenantId);
        CREATE NONCLUSTERED INDEX IX_Medicines_GenericName ON dbo.Medicines (GenericName);
        CREATE NONCLUSTERED INDEX IX_Medicines_BrandName ON dbo.Medicines (BrandName);

        -- Seed the global reference lists.
        INSERT INTO dbo.DosageForms (Name)
        VALUES (N'Tablet'), (N'Capsule'), (N'Syrup'), (N'Injection'), (N'Ointment'),
               (N'Drops'), (N'Cream'), (N'Powder'), (N'Suspension'), (N'Inhaler');

        INSERT INTO dbo.UnitsOfMeasure (Name, ShortCode)
        VALUES (N'Piece', N'pc'), (N'Tablet', N'tab'), (N'Capsule', N'cap'), (N'Strip', N'strip'),
               (N'Box', N'box'), (N'Bottle', N'btl'), (N'Vial', N'vial'), (N'Tube', N'tube'),
               (N'Sachet', N'sachet'), (N'Millilitre', N'ml');

        -- Seed this module's permissions and grant them to Admin only (filtered by
        -- the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Medicines.View', N'View medicines', N'Medicines', N'View the medicine catalog, categories and manufacturers'),
            (N'Medicines.Manage', N'Manage medicines', N'Medicines', N'Create/edit medicines, categories and manufacturers');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Medicines.View', N'Medicines.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.4.0', N'Module 3', N'Medicine Master Data Management: DosageForms/UnitsOfMeasure (global reference lists), MedicineCategories/Manufacturers/Medicines (tenant-scoped), Medicines permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.4.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.4.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.4.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 4: Supplier and Customer Management  (schema version 0.5.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.5.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.5.0 (Module 4: Supplier and Customer Management)...';

        -- Suppliers: wholesalers/distributors the pharmacy buys stock from.
        -- Distinct from dbo.Manufacturers (Module 3), which is the drug company
        -- that made the medicine, not who sold it to this pharmacy.
        CREATE TABLE dbo.Suppliers
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            Name            NVARCHAR(200)           NOT NULL,
            ContactPerson   NVARCHAR(200)           NULL,
            Phone           NVARCHAR(30)            NULL,
            Email           NVARCHAR(256)           NULL,
            Address         NVARCHAR(500)           NULL,
            OpeningBalance  DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Suppliers_OpeningBalance DEFAULT (0),
            CurrentBalance  DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Suppliers_CurrentBalance DEFAULT (0),
            IsActive        BIT                     NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_Suppliers_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc   DATETIME2(0)            NULL,
            CONSTRAINT PK_Suppliers PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Suppliers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Suppliers_TenantId ON dbo.Suppliers (TenantId);
        CREATE NONCLUSTERED INDEX IX_Suppliers_Name ON dbo.Suppliers (Name);

        -- Customers: most sales are anonymous walk-in/cash and never need a row
        -- here. IsWalkInDefault marks the one seeded fallback row per tenant
        -- that Module 7 (POS) attaches anonymous sales to; the app refuses to
        -- deactivate it. OpeningBalance/CurrentBalance follow the same
        -- "initialize now, mutate only from Module 5/7/9 onward" rule as
        -- Suppliers - see BusinessRules.md.
        CREATE TABLE dbo.Customers
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId            BIGINT                  NOT NULL,
            Name                NVARCHAR(200)           NOT NULL,
            Phone               NVARCHAR(30)            NULL,
            Email               NVARCHAR(256)           NULL,
            Address             NVARCHAR(500)           NULL,
            IsCreditEligible    BIT                     NOT NULL CONSTRAINT DF_Customers_IsCreditEligible DEFAULT (0),
            CreditLimit         DECIMAL(18,2)           NULL,
            OpeningBalance      DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Customers_OpeningBalance DEFAULT (0),
            CurrentBalance      DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Customers_CurrentBalance DEFAULT (0),
            IsWalkInDefault     BIT                     NOT NULL CONSTRAINT DF_Customers_IsWalkInDefault DEFAULT (0),
            IsActive            BIT                     NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
            CreatedAtUtc        DATETIME2(0)            NOT NULL CONSTRAINT DF_Customers_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc       DATETIME2(0)            NULL,
            CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Customers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Customers_TenantId ON dbo.Customers (TenantId);
        CREATE NONCLUSTERED INDEX IX_Customers_Name ON dbo.Customers (Name);

        -- Seed one default Walk-in Customer per existing tenant.
        INSERT INTO dbo.Customers (TenantId, Name, IsWalkInDefault, IsActive)
        SELECT Id, N'Walk-in Customer', 1, 1
        FROM dbo.Tenants;

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Suppliers.View', N'View suppliers', N'Suppliers', N'View the list of suppliers'),
            (N'Suppliers.Manage', N'Manage suppliers', N'Suppliers', N'Create/edit suppliers'),
            (N'Customers.View', N'View customers', N'Customers', N'View the list of customers'),
            (N'Customers.Manage', N'Manage customers', N'Customers', N'Create/edit customers');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Suppliers.View', N'Suppliers.Manage', N'Customers.View', N'Customers.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.5.0', N'Module 4', N'Supplier and Customer Management: Suppliers/Customers tables, seeded default Walk-in Customer per tenant, Suppliers/Customers permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.5.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.5.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.5.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 5: Purchase Management  (schema version 0.6.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.6.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.6.0 (Module 5: Purchase Management)...';

        -- Purchases: the financial record of buying stock from a Supplier.
        -- Create-and-view only in the app - no edit screen. Corrections flow
        -- through purchase returns (Module 8), not a silent edit, since this
        -- record has already moved a Supplier balance and created batches.
        CREATE TABLE dbo.Purchases
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            BranchId        BIGINT                  NOT NULL,
            SupplierId      BIGINT                  NOT NULL,
            PurchaseDate    DATETIME2(0)            NOT NULL,
            InvoiceNumber   NVARCHAR(100)           NULL,
            SubTotal        DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Purchases_SubTotal DEFAULT (0),
            DiscountAmount  DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Purchases_DiscountAmount DEFAULT (0),
            TaxAmount       DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Purchases_TaxAmount DEFAULT (0),
            TotalAmount     DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Purchases_TotalAmount DEFAULT (0),
            AmountPaid      DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Purchases_AmountPaid DEFAULT (0),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_Purchases_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_Purchases PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Purchases_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
            CONSTRAINT FK_Purchases_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id),
            CONSTRAINT FK_Purchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Purchases_TenantId ON dbo.Purchases (TenantId);
        CREATE NONCLUSTERED INDEX IX_Purchases_SupplierId ON dbo.Purchases (SupplierId);
        CREATE NONCLUSTERED INDEX IX_Purchases_PurchaseDate ON dbo.Purchases (PurchaseDate);

        -- PurchaseItems: what was invoiced, one row per batch of one medicine.
        -- Quantity is in the medicine's pack unit (matching the supplier
        -- invoice); PharmaDesk.Medicines.PackSize converts it to base units.
        CREATE TABLE dbo.PurchaseItems
        (
            Id                      BIGINT IDENTITY(1,1)   NOT NULL,
            PurchaseId              BIGINT                  NOT NULL,
            MedicineId              BIGINT                  NOT NULL,
            BatchNumber             NVARCHAR(100)           NOT NULL,
            ExpiryDate              DATE                    NOT NULL,
            QuantityPacks           DECIMAL(18,2)           NOT NULL,
            PurchasePricePerPack    DECIMAL(18,2)           NOT NULL,
            SellingPricePerBaseUnit DECIMAL(18,2)           NOT NULL,
            LineTotal               DECIMAL(18,2)           NOT NULL,
            CONSTRAINT PK_PurchaseItems PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_PurchaseItems_Purchases FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchases (Id) ON DELETE CASCADE,
            CONSTRAINT FK_PurchaseItems_Medicines FOREIGN KEY (MedicineId) REFERENCES dbo.Medicines (Id)
        );

        CREATE NONCLUSTERED INDEX IX_PurchaseItems_PurchaseId ON dbo.PurchaseItems (PurchaseId);

        -- MedicineBatches: the stock-tracking record created by a purchase.
        -- QuantityRemaining starts equal to QuantityReceived and is decremented
        -- by Module 7 (POS) sales; Module 6 owns reporting/alerts/adjustments
        -- on top of this table, not its creation - see BusinessRules.md.
        CREATE TABLE dbo.MedicineBatches
        (
            Id                      BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId                BIGINT                  NOT NULL,
            BranchId                BIGINT                  NOT NULL,
            MedicineId              BIGINT                  NOT NULL,
            SupplierId              BIGINT                  NOT NULL,
            PurchaseId              BIGINT                  NOT NULL,
            PurchaseItemId          BIGINT                  NOT NULL,
            BatchNumber             NVARCHAR(100)           NOT NULL,
            ExpiryDate              DATE                    NOT NULL,
            QuantityReceived        DECIMAL(18,2)           NOT NULL,
            QuantityRemaining       DECIMAL(18,2)           NOT NULL,
            PurchaseCostPerBaseUnit DECIMAL(18,2)           NOT NULL,
            SellingPricePerBaseUnit DECIMAL(18,2)           NOT NULL,
            CreatedAtUtc            DATETIME2(0)            NOT NULL CONSTRAINT DF_MedicineBatches_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_MedicineBatches PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_MedicineBatches_Medicines FOREIGN KEY (MedicineId) REFERENCES dbo.Medicines (Id),
            CONSTRAINT FK_MedicineBatches_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id),
            CONSTRAINT FK_MedicineBatches_Purchases FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchases (Id),
            CONSTRAINT FK_MedicineBatches_PurchaseItems FOREIGN KEY (PurchaseItemId) REFERENCES dbo.PurchaseItems (Id)
        );

        CREATE NONCLUSTERED INDEX IX_MedicineBatches_TenantId ON dbo.MedicineBatches (TenantId);
        CREATE NONCLUSTERED INDEX IX_MedicineBatches_MedicineId ON dbo.MedicineBatches (MedicineId);
        CREATE NONCLUSTERED INDEX IX_MedicineBatches_ExpiryDate ON dbo.MedicineBatches (ExpiryDate);

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Purchases.View', N'View purchases', N'Purchases', N'View purchase records and their line items'),
            (N'Purchases.Manage', N'Record purchases', N'Purchases', N'Record new purchases from suppliers');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Purchases.View', N'Purchases.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.6.0', N'Module 5', N'Purchase Management: Purchases/PurchaseItems/MedicineBatches tables, Purchases permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.6.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.6.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.6.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 6: Batch-wise Inventory  (schema version 0.7.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.7.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.7.0 (Module 6: Batch-wise Inventory)...';

        -- StockAdjustments: an audit trail for manual corrections to
        -- MedicineBatches.QuantityRemaining (damage, loss, physical count
        -- correction). Who made the adjustment is already captured by the
        -- app's AuditLogs entry - not duplicated here.
        CREATE TABLE dbo.StockAdjustments
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            BranchId        BIGINT                  NOT NULL,
            MedicineBatchId BIGINT                  NOT NULL,
            QuantityDelta   DECIMAL(18,2)           NOT NULL,
            Reason          NVARCHAR(100)           NOT NULL,
            Notes           NVARCHAR(500)           NULL,
            AdjustedAtUtc   DATETIME2(0)            NOT NULL CONSTRAINT DF_StockAdjustments_AdjustedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_StockAdjustments PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_StockAdjustments_MedicineBatches FOREIGN KEY (MedicineBatchId) REFERENCES dbo.MedicineBatches (Id)
        );

        CREATE NONCLUSTERED INDEX IX_StockAdjustments_TenantId ON dbo.StockAdjustments (TenantId);
        CREATE NONCLUSTERED INDEX IX_StockAdjustments_MedicineBatchId ON dbo.StockAdjustments (MedicineBatchId);

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Inventory.View', N'View inventory', N'Inventory', N'View stock levels, batches, and expiry alerts'),
            (N'Inventory.Adjust', N'Adjust stock', N'Inventory', N'Record manual stock adjustments (damage, loss, correction)');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Inventory.View', N'Inventory.Adjust');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.7.0', N'Module 6', N'Batch-wise Inventory: StockAdjustments table, Inventory permissions. Stock overview/batch list/expiry alerts are read-only views over Module 5''s MedicineBatches - no new tables were needed for them.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.7.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.7.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.7.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 7: POS and Sales Management  (schema version 0.8.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.8.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.8.0 (Module 7: POS and Sales Management)...';

        -- Sales: the financial record of selling to a Customer. Create-and-view
        -- only in the app - no edit screen, same reasoning as dbo.Purchases.
        CREATE TABLE dbo.Sales
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            BranchId        BIGINT                  NOT NULL,
            CustomerId      BIGINT                  NOT NULL,
            SaleDate        DATETIME2(0)            NOT NULL,
            SubTotal        DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Sales_SubTotal DEFAULT (0),
            DiscountAmount  DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Sales_DiscountAmount DEFAULT (0),
            TaxAmount       DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Sales_TaxAmount DEFAULT (0),
            TotalAmount     DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Sales_TotalAmount DEFAULT (0),
            AmountPaid      DECIMAL(18,2)           NOT NULL CONSTRAINT DF_Sales_AmountPaid DEFAULT (0),
            PaymentMethod   NVARCHAR(50)            NULL,
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_Sales_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_Sales PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Sales_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
            CONSTRAINT FK_Sales_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id),
            CONSTRAINT FK_Sales_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Sales_TenantId ON dbo.Sales (TenantId);
        CREATE NONCLUSTERED INDEX IX_Sales_CustomerId ON dbo.Sales (CustomerId);
        CREATE NONCLUSTERED INDEX IX_Sales_SaleDate ON dbo.Sales (SaleDate);

        -- SaleItems: one row per medicine line. Quantity is in BASE units (the
        -- customer buys individual tablets/pieces, not packs) - unlike
        -- PurchaseItems, which is in pack units. UnitPrice is the price quoted
        -- from the soonest-expiring batch at sale time, locked for this line
        -- even if fulfilling it spans into a later batch with a different cost
        -- (see dbo.SaleBatchAllocations and BusinessRules.md).
        CREATE TABLE dbo.SaleItems
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            SaleId              BIGINT                  NOT NULL,
            MedicineId          BIGINT                  NOT NULL,
            QuantityBaseUnits   DECIMAL(18,2)           NOT NULL,
            UnitPrice           DECIMAL(18,2)           NOT NULL,
            LineTotal           DECIMAL(18,2)           NOT NULL,
            CONSTRAINT PK_SaleItems PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales (Id) ON DELETE CASCADE,
            CONSTRAINT FK_SaleItems_Medicines FOREIGN KEY (MedicineId) REFERENCES dbo.Medicines (Id)
        );

        CREATE NONCLUSTERED INDEX IX_SaleItems_SaleId ON dbo.SaleItems (SaleId);

        -- SaleBatchAllocations: exactly which MedicineBatches were FEFO-consumed
        -- to fulfil a SaleItem, and at what actual cost - the COGS traceability
        -- record for Module 11. CostPerBaseUnitAtSale is a snapshot (batches
        -- don't change cost after creation, but this keeps history immutable
        -- regardless).
        CREATE TABLE dbo.SaleBatchAllocations
        (
            Id                      BIGINT IDENTITY(1,1)   NOT NULL,
            SaleItemId              BIGINT                  NOT NULL,
            MedicineBatchId         BIGINT                  NOT NULL,
            QuantityAllocated       DECIMAL(18,2)           NOT NULL,
            CostPerBaseUnitAtSale   DECIMAL(18,2)           NOT NULL,
            CONSTRAINT PK_SaleBatchAllocations PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_SaleBatchAllocations_SaleItems FOREIGN KEY (SaleItemId) REFERENCES dbo.SaleItems (Id) ON DELETE CASCADE,
            CONSTRAINT FK_SaleBatchAllocations_MedicineBatches FOREIGN KEY (MedicineBatchId) REFERENCES dbo.MedicineBatches (Id)
        );

        CREATE NONCLUSTERED INDEX IX_SaleBatchAllocations_SaleItemId ON dbo.SaleBatchAllocations (SaleItemId);
        CREATE NONCLUSTERED INDEX IX_SaleBatchAllocations_MedicineBatchId ON dbo.SaleBatchAllocations (MedicineBatchId);

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Sales.View', N'View sales', N'Sales', N'View sale records and receipts'),
            (N'Sales.Manage', N'Record sales', N'Sales', N'Use the POS screen to record new sales');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Sales.View', N'Sales.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.8.0', N'Module 7', N'POS and Sales Management: Sales/SaleItems/SaleBatchAllocations tables, Sales permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.8.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.8.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.8.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 8: Sales and Purchase Returns  (schema version 0.9.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.9.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.9.0 (Module 8: Sales and Purchase Returns)...';

        -- SalesReturns: a customer returning medicine from a past sale line. The
        -- UNIQUE index on SaleItemId enforces this module's deliberate v1 scope
        -- limit - a given sale line can only be returned once, for up to its
        -- full quantity, never multiple partial returns accumulating against
        -- the same line. See BusinessRules.md.
        CREATE TABLE dbo.SalesReturns
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId            BIGINT                  NOT NULL,
            BranchId            BIGINT                  NOT NULL,
            SaleId              BIGINT                  NOT NULL,
            SaleItemId          BIGINT                  NOT NULL,
            CustomerId          BIGINT                  NOT NULL,
            QuantityReturned    DECIMAL(18,2)           NOT NULL,
            UnitPriceAtSale     DECIMAL(18,2)           NOT NULL,
            ReturnAmount        DECIMAL(18,2)           NOT NULL,
            IsRestocked         BIT                     NOT NULL CONSTRAINT DF_SalesReturns_IsRestocked DEFAULT (1),
            Reason              NVARCHAR(100)           NOT NULL,
            Notes               NVARCHAR(500)           NULL,
            ReturnedAtUtc       DATETIME2(0)            NOT NULL CONSTRAINT DF_SalesReturns_ReturnedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_SalesReturns PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_SalesReturns_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales (Id),
            CONSTRAINT FK_SalesReturns_SaleItems FOREIGN KEY (SaleItemId) REFERENCES dbo.SaleItems (Id),
            CONSTRAINT FK_SalesReturns_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
        );

        CREATE NONCLUSTERED INDEX IX_SalesReturns_TenantId ON dbo.SalesReturns (TenantId);
        CREATE UNIQUE NONCLUSTERED INDEX UQ_SalesReturns_SaleItemId ON dbo.SalesReturns (SaleItemId);

        -- SalesReturnBatchCredits: which MedicineBatches were credited back when a
        -- return's IsRestocked = 1, mirroring (in reverse) the original
        -- SaleBatchAllocations rather than re-deriving FEFO.
        CREATE TABLE dbo.SalesReturnBatchCredits
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            SalesReturnId       BIGINT                  NOT NULL,
            MedicineBatchId     BIGINT                  NOT NULL,
            QuantityCredited    DECIMAL(18,2)           NOT NULL,
            CONSTRAINT PK_SalesReturnBatchCredits PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_SalesReturnBatchCredits_SalesReturns FOREIGN KEY (SalesReturnId) REFERENCES dbo.SalesReturns (Id) ON DELETE CASCADE,
            CONSTRAINT FK_SalesReturnBatchCredits_MedicineBatches FOREIGN KEY (MedicineBatchId) REFERENCES dbo.MedicineBatches (Id)
        );

        CREATE NONCLUSTERED INDEX IX_SalesReturnBatchCredits_SalesReturnId ON dbo.SalesReturnBatchCredits (SalesReturnId);
        CREATE NONCLUSTERED INDEX IX_SalesReturnBatchCredits_MedicineBatchId ON dbo.SalesReturnBatchCredits (MedicineBatchId);

        -- PurchaseReturns: the pharmacy sending stock back to a Supplier. Always
        -- decrements the batch - there is no restock option, since the stock is
        -- physically leaving. Multiple partial returns against the same batch
        -- are fine (each just checks against its current QuantityRemaining).
        CREATE TABLE dbo.PurchaseReturns
        (
            Id                          BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId                    BIGINT                  NOT NULL,
            BranchId                    BIGINT                  NOT NULL,
            PurchaseId                  BIGINT                  NOT NULL,
            MedicineBatchId             BIGINT                  NOT NULL,
            SupplierId                  BIGINT                  NOT NULL,
            QuantityReturned            DECIMAL(18,2)           NOT NULL,
            CostPerBaseUnitAtPurchase   DECIMAL(18,2)           NOT NULL,
            ReturnAmount                DECIMAL(18,2)           NOT NULL,
            Reason                      NVARCHAR(100)           NOT NULL,
            Notes                       NVARCHAR(500)           NULL,
            ReturnedAtUtc               DATETIME2(0)            NOT NULL CONSTRAINT DF_PurchaseReturns_ReturnedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_PurchaseReturns PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_PurchaseReturns_Purchases FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchases (Id),
            CONSTRAINT FK_PurchaseReturns_MedicineBatches FOREIGN KEY (MedicineBatchId) REFERENCES dbo.MedicineBatches (Id),
            CONSTRAINT FK_PurchaseReturns_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id)
        );

        CREATE NONCLUSTERED INDEX IX_PurchaseReturns_TenantId ON dbo.PurchaseReturns (TenantId);
        CREATE NONCLUSTERED INDEX IX_PurchaseReturns_MedicineBatchId ON dbo.PurchaseReturns (MedicineBatchId);

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Returns.View', N'View returns', N'Returns', N'View sales and purchase return records'),
            (N'Returns.Manage', N'Manage returns', N'Returns', N'Record sales and purchase returns');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Returns.View', N'Returns.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.9.0', N'Module 8', N'Sales and Purchase Returns: SalesReturns/SalesReturnBatchCredits/PurchaseReturns tables, Returns permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.9.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.9.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.9.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 9: Due and Payment Management  (schema version 0.10.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.10.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.10.0 (Module 9: Due and Payment Management)...';

        -- CustomerPayments / SupplierPayments: standalone payments against the
        -- running balance, not matched to a specific sale/purchase invoice - a
        -- deliberate scope decision (see BusinessRules.md). Each reduces
        -- Customer/Supplier.CurrentBalance; no cap against overpayment, since a
        -- negative balance (credit owed back) is already valid ledger state as
        -- of Module 8.
        CREATE TABLE dbo.CustomerPayments
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            BranchId        BIGINT                  NOT NULL,
            CustomerId      BIGINT                  NOT NULL,
            Amount          DECIMAL(18,2)           NOT NULL,
            PaymentMethod   NVARCHAR(50)            NULL,
            PaymentDate     DATETIME2(0)            NOT NULL,
            Notes           NVARCHAR(500)           NULL,
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_CustomerPayments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_CustomerPayments PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_CustomerPayments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
        );

        CREATE NONCLUSTERED INDEX IX_CustomerPayments_TenantId ON dbo.CustomerPayments (TenantId);
        CREATE NONCLUSTERED INDEX IX_CustomerPayments_CustomerId ON dbo.CustomerPayments (CustomerId);

        CREATE TABLE dbo.SupplierPayments
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            BranchId        BIGINT                  NOT NULL,
            SupplierId      BIGINT                  NOT NULL,
            Amount          DECIMAL(18,2)           NOT NULL,
            PaymentMethod   NVARCHAR(50)            NULL,
            PaymentDate     DATETIME2(0)            NOT NULL,
            Notes           NVARCHAR(500)           NULL,
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_SupplierPayments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_SupplierPayments PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_SupplierPayments_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id)
        );

        CREATE NONCLUSTERED INDEX IX_SupplierPayments_TenantId ON dbo.SupplierPayments (TenantId);
        CREATE NONCLUSTERED INDEX IX_SupplierPayments_SupplierId ON dbo.SupplierPayments (SupplierId);

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Payments.View', N'View due and payments', N'Payments', N'View accounts receivable/payable and statements'),
            (N'Payments.Manage', N'Record payments', N'Payments', N'Record customer and supplier payments');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Payments.View', N'Payments.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.10.0', N'Module 9', N'Due and Payment Management: CustomerPayments/SupplierPayments tables, Payments permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.10.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.10.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.10.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 10: Expense and Cash Management  (schema version 0.11.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.11.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.11.0 (Module 10: Expense and Cash Management)...';

        CREATE TABLE dbo.ExpenseCategories
        (
            Id              BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId        BIGINT                  NOT NULL,
            Name            NVARCHAR(200)           NOT NULL,
            Description     NVARCHAR(500)           NULL,
            IsActive        BIT                     NOT NULL CONSTRAINT DF_ExpenseCategories_IsActive DEFAULT (1),
            CreatedAtUtc    DATETIME2(0)            NOT NULL CONSTRAINT DF_ExpenseCategories_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc   DATETIME2(0)            NULL,
            CONSTRAINT PK_ExpenseCategories PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_ExpenseCategories_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
        );

        CREATE NONCLUSTERED INDEX IX_ExpenseCategories_TenantId ON dbo.ExpenseCategories (TenantId);

        -- Expenses: unlike Purchases/Sales/Returns, these support Edit and Void
        -- (soft-deactivate) rather than being create-and-view only - an expense
        -- doesn't move stock or any party's balance, so there's nothing else to
        -- reverse. See BusinessRules.md.
        CREATE TABLE dbo.Expenses
        (
            Id                  BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId            BIGINT                  NOT NULL,
            BranchId            BIGINT                  NOT NULL,
            ExpenseCategoryId   BIGINT                  NOT NULL,
            Amount              DECIMAL(18,2)           NOT NULL,
            PaymentMethod       NVARCHAR(50)            NULL,
            ExpenseDate         DATETIME2(0)            NOT NULL,
            Notes               NVARCHAR(500)           NULL,
            IsActive            BIT                     NOT NULL CONSTRAINT DF_Expenses_IsActive DEFAULT (1),
            CreatedAtUtc        DATETIME2(0)            NOT NULL CONSTRAINT DF_Expenses_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            ModifiedAtUtc       DATETIME2(0)            NULL,
            CONSTRAINT PK_Expenses PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_Expenses_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
            CONSTRAINT FK_Expenses_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id),
            CONSTRAINT FK_Expenses_ExpenseCategories FOREIGN KEY (ExpenseCategoryId) REFERENCES dbo.ExpenseCategories (Id)
        );

        CREATE NONCLUSTERED INDEX IX_Expenses_TenantId ON dbo.Expenses (TenantId);
        CREATE NONCLUSTERED INDEX IX_Expenses_BranchId ON dbo.Expenses (BranchId);
        CREATE NONCLUSTERED INDEX IX_Expenses_ExpenseDate ON dbo.Expenses (ExpenseDate);

        -- CashDrawerSessions: one open session per branch at a time (enforced at
        -- the app level, not a DB constraint, since "open" is ClosedAtUtc IS NULL
        -- rather than a simple unique column). ExpectedClosingCashAmount is
        -- computed from cash Sales/CustomerPayments/Expenses/SupplierPayments for
        -- that branch and date - it deliberately excludes Sales/Purchase Returns,
        -- which never recorded a cash/non-cash settlement marker. See
        -- BusinessRules.md.
        CREATE TABLE dbo.CashDrawerSessions
        (
            Id                          BIGINT IDENTITY(1,1)   NOT NULL,
            TenantId                    BIGINT                  NOT NULL,
            BranchId                    BIGINT                  NOT NULL,
            SessionDate                 DATE                    NOT NULL,
            OpeningCashAmount           DECIMAL(18,2)           NOT NULL,
            ClosingCashAmount           DECIMAL(18,2)           NULL,
            ExpectedClosingCashAmount   DECIMAL(18,2)           NULL,
            Variance                    DECIMAL(18,2)           NULL,
            Notes                       NVARCHAR(500)           NULL,
            OpenedAtUtc                 DATETIME2(0)            NOT NULL CONSTRAINT DF_CashDrawerSessions_OpenedAtUtc DEFAULT (SYSUTCDATETIME()),
            ClosedAtUtc                 DATETIME2(0)            NULL,
            CONSTRAINT PK_CashDrawerSessions PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT FK_CashDrawerSessions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
            CONSTRAINT FK_CashDrawerSessions_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id)
        );

        CREATE NONCLUSTERED INDEX IX_CashDrawerSessions_TenantId ON dbo.CashDrawerSessions (TenantId);
        CREATE NONCLUSTERED INDEX IX_CashDrawerSessions_BranchId ON dbo.CashDrawerSessions (BranchId);

        -- Seed this module's permissions and grant them to Admin only (filtered
        -- by the new codes specifically - see the Module 2 block above for why).
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Expenses.View', N'View expenses', N'Expenses', N'View expense categories and records'),
            (N'Expenses.Manage', N'Manage expenses', N'Expenses', N'Create/edit/void expenses and categories'),
            (N'CashDrawer.View', N'View cash drawer', N'CashDrawer', N'View cash drawer status and history'),
            (N'CashDrawer.Manage', N'Manage cash drawer', N'CashDrawer', N'Open and close cash drawer sessions');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Expenses.View', N'Expenses.Manage', N'CashDrawer.View', N'CashDrawer.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.11.0', N'Module 10', N'Expense and Cash Management: ExpenseCategories/Expenses/CashDrawerSessions tables, Expenses/CashDrawer permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.11.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.11.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.11.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 11: Reports and Analytics  (schema version 0.12.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.12.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.12.0 (Module 11: Reports and Analytics)...';

        -- No new tables: Sales/Purchase/Profit & Loss/Inventory Valuation reports
        -- are read-only queries over existing tables. Only a new permission is
        -- seeded here.
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Reports.View', N'View reports', N'Reports', N'View sales, purchase, profit and loss, and inventory valuation reports');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Reports.View');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.12.0', N'Module 11', N'Reports and Analytics: Reports.View permission. No new tables - Sales/Purchase/Profit & Loss/Inventory Valuation reports read from existing tables.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.12.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.12.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.12.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 12: Dashboard and Notifications  (schema version 0.13.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.13.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.13.0 (Module 12: Dashboard and Notifications)...';

        -- No schema changes at all: the real dashboard KPIs/chart and the
        -- notification bell are read-only over existing tables and reuse
        -- existing permissions (Sales.View, Payments.View, Inventory.View,
        -- CashDrawer.View) to decide what each viewer sees. This version row
        -- exists purely so the module history stays complete and traceable.
        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.13.0', N'Module 12', N'Dashboard and Notifications: no schema changes. Real KPI/chart dashboard and a live-computed notification bell, both reading existing tables and reusing existing permissions.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.13.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.13.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.13.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 13: Backup and Restore  (schema version 0.14.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.14.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.14.0 (Module 13: Backup and Restore)...';

        -- No new tables: backup files are tracked on disk (the configured
        -- BackupSettings:DirectoryPath), not mirrored into a database table,
        -- so there is no possibility of the two drifting out of sync. Only
        -- the new permissions are seeded here.
        INSERT INTO dbo.Permissions (Code, Name, ModuleName, Description)
        VALUES
            (N'Backup.View', N'View backups', N'Backup', N'View, download, and see restore instructions for backups'),
            (N'Backup.Manage', N'Manage backups', N'Backup', N'Create and delete backups');

        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT r.Id, p.Id
        FROM dbo.Roles r
        CROSS JOIN dbo.Permissions p
        WHERE r.NormalizedName = N'ADMIN'
          AND p.Code IN (N'Backup.View', N'Backup.Manage');

        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.14.0', N'Module 13', N'Backup and Restore: Backup.View/Backup.Manage permissions. No new tables - backup files are tracked on disk, not in the database. The app creates backups (BACKUP DATABASE) and a scheduled daily job; it deliberately does not execute RESTORE DATABASE itself - see BusinessRules.md.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.14.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.14.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.14.0 already applied - skipping.';
END
GO

-- ============================================================================
-- MODULE 14: Testing, Security Hardening and Deployment  (schema version 0.15.0)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = N'0.15.0')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        PRINT 'Applying schema version 0.15.0 (Module 14: Testing, Security Hardening and Deployment)...';

        -- No schema changes: this module's work (security headers, login rate
        -- limiting, cookie hardening, final test pass, deployment docs) is all
        -- application-layer or documentation. This version row exists purely so
        -- the module history (Modules 0-14, the full original master plan)
        -- stays complete and traceable.
        INSERT INTO dbo.DatabaseSchemaVersions (VersionNumber, ModuleName, Description)
        VALUES (N'0.15.0', N'Module 14', N'Testing, Security Hardening and Deployment: no schema changes. Security response headers, login-endpoint rate limiting, production cookie hardening, a final full test-suite pass, and the completed DeploymentGuide.md.');

        COMMIT TRANSACTION;
        PRINT 'Schema version 0.15.0 applied successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        PRINT 'ERROR applying schema version 0.15.0: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Schema version 0.15.0 already applied - skipping.';
END
GO

-- ============================================================================
-- All 15 modules (0-14) from the original master development plan are now
-- complete. Any future module appends its own "IF NOT EXISTS (... version
-- X.Y.Z ...)" block below this line. Do not edit or remove the blocks above.
-- ============================================================================

PRINT 'PharmaDesk_Complete.sql finished. Current schema version: 0.15.0';
GO
