-- Simple Page Setup Script
-- This ensures all pages exist in the database
-- Admin users will automatically see all pages without needing permissions!

USE [QES_KUKA_AMR_Penang];
GO

-- Clear and re-insert pages with correct paths
DELETE FROM RolePermissions;
DELETE FROM [Page];

PRINT 'Cleared existing Pages and RolePermissions';

-- Insert correct page paths (matching actual .cshtml files)
INSERT INTO [Page] (PageName, PagePath, CreatedAt) VALUES
('Dashboard', '/Index', GETUTCDATE()),
('Workflow Management', '/WorkflowManagement', GETUTCDATE()),
('Workflow Monitor', '/WorkflowMonitor', GETUTCDATE()),
('Workflow Trigger', '/WorkflowTrigger', GETUTCDATE()),
('Queue Monitor', '/QueueMonitor', GETUTCDATE()),
('Mission History', '/MissionHistory', GETUTCDATE()),
('Mission Configuration', '/MissionConfiguration', GETUTCDATE()),
('Custom Mission', '/CustomMission', GETUTCDATE()),
('Saved Custom Missions', '/SavedCustomMissions', GETUTCDATE()),
('QR Code Management', '/QrCode', GETUTCDATE()),
('Robot List', '/RobotList', GETUTCDATE()),
('Area Management', '/Area', GETUTCDATE()),
('Robot Utilization', '/Analytics/RobotUtilization', GETUTCDATE()),
('User List', '/UserList', GETUTCDATE()),
('Role & Permission Config', '/RolePermissionConfig', GETUTCDATE()),
('Log Retention', '/Settings/LogRetention', GETUTCDATE());

PRINT 'Pages seeded successfully!';
PRINT '';
PRINT '=== Pages Created ===';
SELECT Id, PageName, PagePath FROM [Page] ORDER BY Id;

PRINT '';
PRINT '=== IMPORTANT ===';
PRINT 'Admin users automatically see ALL pages - no permissions needed!';
PRINT 'Non-admin users need permissions assigned in Role & Permission Config.';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Restart your web application';
PRINT '2. Login with an admin account (any user with "Admin" or "Administrator" role)';
PRINT '3. You will see all pages immediately!';
PRINT '==================';
GO
