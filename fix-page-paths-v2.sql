-- Fix incorrect page paths - Clear and reseed (Version 2)
-- This script prepares the database but you MUST sync users after running it
-- Run this to update the Pages table with correct paths

USE [QES_KUKA_AMR_Penang];
GO

-- Clear existing data (this will cascade to RolePermissions due to FK)
DELETE FROM RolePermissions;
DELETE FROM [Page];
-- Keep Roles table - it will be populated by User Sync

PRINT 'Cleared existing Pages and RolePermissions';
PRINT 'Roles table preserved for User Sync';

-- Insert correct page paths (matching actual .cshtml files)
DECLARE @PageData TABLE (PageName NVARCHAR(200), PagePath NVARCHAR(200));

INSERT INTO @PageData (PageName, PagePath) VALUES
('Dashboard', '/Index'),
('Workflow Management', '/WorkflowManagement'),
('Workflow Monitor', '/WorkflowMonitor'),
('Workflow Trigger', '/WorkflowTrigger'),
('Queue Monitor', '/QueueMonitor'),
('Mission History', '/MissionHistory'),
('Mission Configuration', '/MissionConfiguration'),
('Custom Mission', '/CustomMission'),
('Saved Custom Missions', '/SavedCustomMissions'),
('QR Code Management', '/QrCode'),
('Robot List', '/RobotList'),
('Area Management', '/Area'),
('Robot Utilization', '/Analytics/RobotUtilization'),
('User List', '/UserList'),
('Role & Permission Config', '/RolePermissionConfig'),
('Log Retention', '/Settings/LogRetention');

-- Insert pages
INSERT INTO [Page] (PageName, PagePath, CreatedAt)
SELECT pd.PageName, pd.PagePath, GETUTCDATE()
FROM @PageData pd;

PRINT 'Pages seeded with correct paths';

-- Verification
SELECT 
    'Results' AS [Status],
    (SELECT COUNT(*) FROM Roles) AS TotalRoles,
    (SELECT COUNT(*) FROM [Page]) AS TotalPages,
    (SELECT COUNT(*) FROM RolePermissions) AS TotalRolePermissions;

PRINT '';
PRINT '=== IMPORTANT NEXT STEPS ===';
PRINT '1. Start your API application';
PRINT '2. Login to the web application';
PRINT '3. Go to User List page and click "Sync Users"';
PRINT '   - This will populate the Roles table with your actual roles';
PRINT '4. Go to Role & Permission Config page';
PRINT '5. Assign pages to your roles (e.g., administrator, normal, etc.)';
PRINT '6. Logout and Login again';
PRINT '7. You should now see all the pages in the sidebar';
PRINT '============================';
GO
