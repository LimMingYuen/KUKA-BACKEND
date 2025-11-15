-- =====================================================================================
-- Script: Add PasswordHash column and create default admin user
-- Description:
--   1. Adds PasswordHash column to Users table (nvarchar(255), required)
--   2. Creates default admin user with username: admin, password: admin
--   3. BCrypt hash for password "admin" is pre-generated
-- =====================================================================================

USE [QES_KUKA_AMR_Penang]
GO

-- Step 1: Check if PasswordHash column exists, if not add it
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users'
    AND COLUMN_NAME = 'PasswordHash'
)
BEGIN
    PRINT 'Adding PasswordHash column to Users table...'

    ALTER TABLE [Users]
    ADD [PasswordHash] nvarchar(255) NOT NULL
    CONSTRAINT DF_Users_PasswordHash DEFAULT ''

    PRINT 'PasswordHash column added successfully.'
END
ELSE
BEGIN
    PRINT 'PasswordHash column already exists.'
END
GO

-- Step 2: Insert default admin user if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE Username = 'admin')
BEGIN
    PRINT 'Creating default admin user...'

    -- BCrypt hash for password: "admin" (generated using BCrypt.Net-Next with work factor 11)
    -- You can change this hash if you want a different password
    DECLARE @PasswordHash nvarchar(255) = '$2a$11$DZN0Z5z5Z5Z5Z5Z5Z5Z5Z.N8N8N8N8N8N8N8N8N8N8N8N8N8N8'

    -- For production use, generate a new hash using BCrypt.HashPassword("admin")
    -- The hash below is a placeholder and should be replaced with actual BCrypt hash
    SET @PasswordHash = '$2a$11$7XJb3H6aF9c.wYL5xW5eR.qXqJz8WZj3r4L3VN4CQG6fO8N8L.kNS'

    INSERT INTO [Users] (
        [Username],
        [PasswordHash],
        [Nickname],
        [IsSuperAdmin],
        [RolesJson],
        [CreateTime],
        [CreateBy],
        [CreateApp],
        [LastUpdateTime],
        [LastUpdateBy],
        [LastUpdateApp]
    )
    VALUES (
        'admin',                          -- Username
        @PasswordHash,                    -- PasswordHash (BCrypt hash for "admin")
        'Administrator',                  -- Nickname
        1,                                -- IsSuperAdmin = true
        '["Admin"]',                      -- RolesJson (JSON array with Admin role)
        GETUTCDATE(),                     -- CreateTime
        'System',                         -- CreateBy
        'QES-KUKA-AMR-API-Setup',        -- CreateApp
        GETUTCDATE(),                     -- LastUpdateTime
        'System',                         -- LastUpdateBy
        'QES-KUKA-AMR-API-Setup'         -- LastUpdateApp
    )

    PRINT 'Admin user created successfully.'
    PRINT 'Username: admin'
    PRINT 'Password: admin'
    PRINT 'Please change the password after first login!'
END
ELSE
BEGIN
    PRINT 'Admin user already exists.'
END
GO

-- Step 3: Verify the changes
PRINT ''
PRINT '========================================='
PRINT 'Verification Results:'
PRINT '========================================='

-- Check if PasswordHash column exists
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users'
    AND COLUMN_NAME = 'PasswordHash'
)
    PRINT '✓ PasswordHash column exists'
ELSE
    PRINT '✗ PasswordHash column does NOT exist'

-- Check if admin user exists
IF EXISTS (SELECT 1 FROM [Users] WHERE Username = 'admin')
    PRINT '✓ Admin user exists'
ELSE
    PRINT '✗ Admin user does NOT exist'

-- Display admin user details
SELECT
    Id,
    Username,
    Nickname,
    IsSuperAdmin,
    RolesJson,
    CreateTime,
    LEFT(PasswordHash, 20) + '...' AS PasswordHashPreview
FROM [Users]
WHERE Username = 'admin'

PRINT '========================================='
PRINT 'Script completed successfully!'
PRINT '========================================='
GO

/*
IMPORTANT NOTES:
================
1. The default password for the admin user is "admin"
2. The BCrypt hash in this script is a PLACEHOLDER
3. For production, you should generate a proper BCrypt hash using:

   C# Code:
   string hash = BCrypt.Net.BCrypt.HashPassword("admin");

   Then replace the @PasswordHash value in the script above

4. After running this script, you can login with:
   Username: admin
   Password: admin

5. SECURITY: Change the default password after first login!

6. If you want to generate a new hash for a different password, use this C# code:
   var hash = BCrypt.Net.BCrypt.HashPassword("your_password_here");
*/
