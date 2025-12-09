-- =============================================
-- QES KUKA AMR - Database Reseed Script
-- =============================================
-- This script:
-- 1. Deletes ALL data from all tables (in correct FK order)
-- 2. Reseeds identity columns to 1
-- 3. Seeds default ADMIN role and admin user
-- =============================================
-- WARNING: This will DELETE ALL DATA in the database!
-- =============================================

USE QES_KUKA_AMR_Penang;
GO

SET NOCOUNT ON;

PRINT '=============================================';
PRINT 'Starting database reseed...';
PRINT 'WARNING: All data will be deleted!';
PRINT '=============================================';

-- =============================================
-- Step 1: Delete all data in correct FK order
-- =============================================
-- Must delete child tables before parent tables

PRINT 'Deleting all data...';

-- Level 1: Tables with FK to Users (must delete first)
DELETE FROM RefreshTokens;
PRINT '  - RefreshTokens cleared';

DELETE FROM UserTemplatePermissions;
PRINT '  - UserTemplatePermissions cleared';

DELETE FROM UserPermissions;
PRINT '  - UserPermissions cleared';

-- Level 1: Tables with FK to Roles
DELETE FROM RoleTemplatePermissions;
PRINT '  - RoleTemplatePermissions cleared';

DELETE FROM RolePermissions;
PRINT '  - RolePermissions cleared';

-- Level 1: Tables with FK to SavedCustomMissions (MUST delete before SavedCustomMissions)
DELETE FROM WorkflowSchedules;
PRINT '  - WorkflowSchedules cleared';

-- Level 2: Now can delete SavedCustomMissions (after WorkflowSchedules deleted)
DELETE FROM SavedCustomMissions;
PRINT '  - SavedCustomMissions cleared';

-- Level 2: Other mission/workflow tables (no FK dependencies on them)
DELETE FROM MissionQueues;
PRINT '  - MissionQueues cleared';

DELETE FROM MissionHistories;
PRINT '  - MissionHistories cleared';

DELETE FROM WorkflowZoneMappings;
PRINT '  - WorkflowZoneMappings cleared';

DELETE FROM WorkflowNodeCodes;
PRINT '  - WorkflowNodeCodes cleared';

DELETE FROM WorkflowDiagrams;
PRINT '  - WorkflowDiagrams cleared';

-- Level 2: Robot related tables
DELETE FROM RobotManualPauses;
PRINT '  - RobotManualPauses cleared';

DELETE FROM MobileRobots;
PRINT '  - MobileRobots cleared';

DELETE FROM RobotMonitoringMaps;
PRINT '  - RobotMonitoringMaps cleared';

-- Level 2: Location/mapping tables
DELETE FROM QrCodes;
PRINT '  - QrCodes cleared';

DELETE FROM MapZones;
PRINT '  - MapZones cleared';

-- Level 2: Configuration tables
DELETE FROM MissionTypes;
PRINT '  - MissionTypes cleared';

DELETE FROM RobotTypes;
PRINT '  - RobotTypes cleared';

DELETE FROM ShelfDecisionRules;
PRINT '  - ShelfDecisionRules cleared';

DELETE FROM ResumeStrategies;
PRINT '  - ResumeStrategies cleared';

DELETE FROM Areas;
PRINT '  - Areas cleared';

-- Level 2: System tables
DELETE FROM SystemSetting;
PRINT '  - SystemSetting cleared';

-- Level 3: User management parent tables (after all children deleted)
DELETE FROM Users;
PRINT '  - Users cleared';

DELETE FROM Roles;
PRINT '  - Roles cleared';

DELETE FROM Pages;
PRINT '  - Pages cleared';

PRINT 'All data deleted.';
GO

-- =============================================
-- Step 2: Reseed identity columns to 1
-- =============================================
PRINT 'Reseeding identity columns...';

DBCC CHECKIDENT ('RefreshTokens', RESEED, 0);
DBCC CHECKIDENT ('UserTemplatePermissions', RESEED, 0);
DBCC CHECKIDENT ('UserPermissions', RESEED, 0);
DBCC CHECKIDENT ('RoleTemplatePermissions', RESEED, 0);
DBCC CHECKIDENT ('RolePermissions', RESEED, 0);
DBCC CHECKIDENT ('WorkflowSchedules', RESEED, 0);
DBCC CHECKIDENT ('SavedCustomMissions', RESEED, 0);
DBCC CHECKIDENT ('MissionQueues', RESEED, 0);
DBCC CHECKIDENT ('MissionHistories', RESEED, 0);
DBCC CHECKIDENT ('WorkflowZoneMappings', RESEED, 0);
DBCC CHECKIDENT ('WorkflowNodeCodes', RESEED, 0);
DBCC CHECKIDENT ('WorkflowDiagrams', RESEED, 0);
DBCC CHECKIDENT ('RobotManualPauses', RESEED, 0);
DBCC CHECKIDENT ('MobileRobots', RESEED, 0);
DBCC CHECKIDENT ('RobotMonitoringMaps', RESEED, 0);
DBCC CHECKIDENT ('QrCodes', RESEED, 0);
DBCC CHECKIDENT ('MapZones', RESEED, 0);
DBCC CHECKIDENT ('MissionTypes', RESEED, 0);
DBCC CHECKIDENT ('RobotTypes', RESEED, 0);
DBCC CHECKIDENT ('ShelfDecisionRules', RESEED, 0);
DBCC CHECKIDENT ('ResumeStrategies', RESEED, 0);
DBCC CHECKIDENT ('Areas', RESEED, 0);
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Roles', RESEED, 0);
DBCC CHECKIDENT ('Pages', RESEED, 0);

PRINT 'Identity columns reseeded.';
GO

-- =============================================
-- Step 4: Seed default data
-- =============================================
PRINT 'Seeding default data...';

DECLARE @CurrentUtc DATETIME2 = GETUTCDATE();

-- Insert default ADMIN role
INSERT INTO Roles (Name, RoleCode, IsProtected, CreatedUtc, UpdatedUtc)
VALUES (
    'Administrator',
    'ADMIN',
    1,  -- IsProtected = true
    @CurrentUtc,
    @CurrentUtc
);
PRINT '  - ADMIN role created';

-- NOTE: Admin user will be created automatically by the API on first startup
-- DbInitializer.SeedAsync() generates a proper BCrypt hash for password "admin"
-- Do NOT create admin user here with a static hash - BCrypt requires runtime generation
PRINT '  - Admin user will be created by API on first startup';

-- Insert default system setting for log retention
INSERT INTO SystemSetting ([Key], [Value], LastUpdated)
VALUES ('LogRetentionMonths', '1', @CurrentUtc);
PRINT '  - Default system settings created';

PRINT 'Default data seeded.';
GO

-- =============================================
-- Step 5: Verify the reset
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'Database reseed completed successfully!';
PRINT '=============================================';
PRINT '';
PRINT 'Default credentials:';
PRINT '  Username: admin';
PRINT '  Password: admin';
PRINT '';
PRINT 'IMPORTANT: Change the admin password after first login!';
PRINT '=============================================';

-- Show record counts for verification
SELECT 'Users' AS TableName, COUNT(*) AS RecordCount FROM Users
UNION ALL SELECT 'Roles', COUNT(*) FROM Roles
UNION ALL SELECT 'Pages', COUNT(*) FROM Pages
UNION ALL SELECT 'SystemSetting', COUNT(*) FROM SystemSetting
UNION ALL SELECT 'MissionHistories', COUNT(*) FROM MissionHistories
UNION ALL SELECT 'WorkflowDiagrams', COUNT(*) FROM WorkflowDiagrams
UNION ALL SELECT 'MobileRobots', COUNT(*) FROM MobileRobots
UNION ALL SELECT 'QrCodes', COUNT(*) FROM QrCodes
ORDER BY TableName;
GO
