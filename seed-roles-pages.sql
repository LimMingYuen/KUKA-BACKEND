-- Seed script for Roles, Pages, and RolePermissions
-- This script sets up the Admin role with access to all pages

USE [QES_KUKA_AMR_Penang];
GO

-- Insert Roles (if not exists)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'ADMIN')
BEGIN
    INSERT INTO Roles (Id, Name, RoleCode, IsProtected)
    VALUES (1, 'Admin', 'ADMIN', 1);
    PRINT 'Admin role created';
END
ELSE
BEGIN
    PRINT 'Admin role already exists';
END

-- Insert Pages (all available pages in the application)
-- Check and insert only if not exists
DECLARE @PageData TABLE (PageName NVARCHAR(200), PagePath NVARCHAR(200));

INSERT INTO @PageData (PageName, PagePath) VALUES
('Dashboard', '/Index'),
('Workflow Management', '/WorkflowManagement'),
('Workflow Monitor', '/WorkflowMonitor'),
('Workflow Trigger', '/WorkflowTrigger'),
('Queue Monitor', '/QueueMonitor'),
('Mission History', '/MissionHistory'),
('Mission Configuration', '/MissionConfiguration'),
('Custom Missions', '/CustomMission'),
('Saved Custom Missions', '/SavedCustomMissions'),
('QR Code Management', '/QrCode'),
('Robot List', '/RobotList'),
('Area Management', '/Area'),
('Robot Utilization Analytics', '/Analytics/RobotUtilization'),
('User Management', '/UserList'),
('Role & Permission Config', '/RolePermissionConfig'),
('Log Retention Settings', '/Settings/LogRetention');

-- Insert pages that don't exist
INSERT INTO [Page] (PageName, PagePath, CreatedAt)
SELECT pd.PageName, pd.PagePath, GETUTCDATE()
FROM @PageData pd
WHERE NOT EXISTS (
    SELECT 1 FROM [Page] p 
    WHERE p.PagePath = pd.PagePath
);

PRINT 'Pages seeded';

-- Get the Admin role ID
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = Id FROM Roles WHERE RoleCode = 'ADMIN';

-- Assign all pages to Admin role (if not already assigned)
INSERT INTO RolePermissions (RoleId, PageId)
SELECT @AdminRoleId, p.Id
FROM [Page] p
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = @AdminRoleId AND rp.PageId = p.Id
);

PRINT 'RolePermissions seeded - Admin has access to all pages';

-- Verify the setup
SELECT 
    'Verification Results' AS [Status],
    (SELECT COUNT(*) FROM Roles) AS TotalRoles,
    (SELECT COUNT(*) FROM [Page]) AS TotalPages,
    (SELECT COUNT(*) FROM RolePermissions WHERE RoleId = @AdminRoleId) AS AdminPermissions;

-- Show what pages Admin can access
SELECT 
    r.Name AS RoleName,
    p.PageName,
    p.PagePath
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.Id
INNER JOIN [Page] p ON rp.PageId = p.Id
WHERE r.RoleCode = 'ADMIN'
ORDER BY p.PageName;

PRINT 'Seed script completed successfully!';
GO
