-- =============================================
-- QES KUKA AMR - Admin User Setup Script
-- =============================================
-- This script creates:
-- 1. Admin role in Roles table
-- 2. Admin user in Users table
-- 3. Associates admin user with admin role
-- =============================================

USE QES_KUKA_AMR_Penang;
GO

-- =============================================
-- IMPORTANT NOTE:
-- The User entity currently does NOT have a Password field.
-- You will need to:
-- 1. Add a PasswordHash field to the User entity
-- 2. Create and run a migration to add the column
-- 3. Update this script to include password hashing
-- =============================================

DECLARE @CurrentUtc DATETIME2 = GETUTCDATE();

-- =============================================
-- Step 1: Insert Admin Role
-- =============================================
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'ADMIN')
BEGIN
    INSERT INTO Roles (Name, RoleCode, IsProtected, CreatedUtc, UpdatedUtc)
    VALUES (
        'Administrator',           -- Name
        'ADMIN',                   -- RoleCode (unique)
        1,                         -- IsProtected (true - system role, cannot be deleted)
        @CurrentUtc,               -- CreatedUtc
        @CurrentUtc                -- UpdatedUtc
    );
    PRINT 'Admin role created successfully';
END
ELSE
BEGIN
    PRINT 'Admin role already exists';
END
GO

-- =============================================
-- Step 2: Insert Admin User
-- =============================================
DECLARE @CurrentUtc DATETIME2 = GETUTCDATE();

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (
        Username,
        Nickname,
        IsSuperAdmin,
        RolesJson,
        CreateTime,
        CreateBy,
        CreateApp,
        LastUpdateTime,
        LastUpdateBy,
        LastUpdateApp
    )
    VALUES (
        'admin',                    -- Username
        'System Administrator',     -- Nickname
        1,                          -- IsSuperAdmin (true)
        '["ADMIN"]',                -- RolesJson (JSON array containing ADMIN role)
        @CurrentUtc,                -- CreateTime
        'SYSTEM',                   -- CreateBy
        'SETUP_SCRIPT',             -- CreateApp
        @CurrentUtc,                -- LastUpdateTime
        'SYSTEM',                   -- LastUpdateBy
        'SETUP_SCRIPT'              -- LastUpdateApp
    );
    PRINT 'Admin user created successfully';
    PRINT 'WARNING: Password field is not available. You need to add password support.';
END
ELSE
BEGIN
    PRINT 'Admin user already exists';
END
GO

-- =============================================
-- Step 3: Verify Setup
-- =============================================
SELECT
    u.Id,
    u.Username,
    u.Nickname,
    u.IsSuperAdmin,
    u.RolesJson,
    u.CreateTime,
    u.CreateBy
FROM Users u
WHERE u.Username = 'admin';

SELECT
    r.Id,
    r.Name,
    r.RoleCode,
    r.IsProtected,
    r.CreatedUtc
FROM Roles r
WHERE r.RoleCode = 'ADMIN';

PRINT '=============================================';
PRINT 'Admin user and role setup completed';
PRINT '=============================================';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Add PasswordHash field to User entity';
PRINT '2. Add PasswordSalt field to User entity (recommended)';
PRINT '3. Create migration: dotnet ef migrations add AddUserPassword --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj';
PRINT '4. Update database: dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj';
PRINT '5. Update admin user with hashed password';
PRINT '=============================================';
GO
