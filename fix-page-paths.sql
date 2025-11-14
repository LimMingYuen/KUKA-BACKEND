-- Fix incorrect page paths - Clear and reseed
-- Run this to update the Pages table with correct paths

USE [QES_KUKA_AMR_Penang];
GO

-- Clear existing data (this will cascade to RolePermissions due to FK)
DELETE FROM RolePermissions;
DELETE FROM [Page];
DELETE FROM Roles;

PRINT 'Cleared existing Roles, Pages, and RolePermissions';

-- Re-insert Admin role
INSERT INTO Roles (Id, Name, RoleCode, IsProtected)
VALUES (1, 'Admin', 'ADMIN', 1);

PRINT 'Admin role created';

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

-- Get the Admin role ID
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = Id FROM Roles WHERE RoleCode = 'ADMIN';

-- Assign all pages to Admin role
INSERT INTO RolePermissions (RoleId, PageId)
SELECT @AdminRoleId, p.Id
FROM [Page] p;

PRINT 'RolePermissions seeded - Admin has access to all pages';

-- Verification
SELECT 
    'Results' AS [Status],
    (SELECT COUNT(*) FROM Roles) AS TotalRoles,
    (SELECT COUNT(*) FROM [Page]) AS TotalPages,
    (SELECT COUNT(*) FROM RolePermissions WHERE RoleId = @AdminRoleId) AS AdminPermissions;

-- Show pages Admin can access
SELECT 
    r.Name AS RoleName,
    p.PageName,
    p.PagePath
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.Id
INNER JOIN [Page] p ON rp.PageId = p.Id
WHERE r.RoleCode = 'ADMIN'
ORDER BY p.PageName;

PRINT 'Fix completed! Restart your web application and login again.';
GO
