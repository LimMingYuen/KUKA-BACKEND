# Admin Pages Visibility Fix

## Problem
Admin users couldn't see any pages in the navigation menu after logging in.

## Root Cause
The `_DashboardLayout.cshtml` queries the database for pages based on role permissions:
```csharp
accessiblePages = (from rp in _db.RolePermissions
                   join r in _db.Roles on rp.RoleId equals r.Id
                   join p in _db.Pages on rp.PageId equals p.Id
                   where roleNames.Contains(r.Name)
                   select p)
```

The database tables (`Roles`, `Pages`, `RolePermissions`) were empty, so no pages were returned.

## Solution Implemented

### 1. Database Seeding Service Enhanced
Updated `DatabaseInitializationService.cs` to seed:
- **Roles**: Admin role (RoleCode: "ADMIN")
- **Pages**: All 14 application pages
- **RolePermissions**: Admin role linked to all pages

### 2. Automatic Seeding on Startup
Modified `Program.cs` to:
- Register `IDatabaseInitializationService`
- Call seeding automatically when the application starts
- Handle errors gracefully without crashing the app

### 3. SQL Script Created
Created `seed-roles-pages.sql` for manual database seeding if needed.

## Pages Seeded
1. Dashboard (`/Index`)
2. Workflow Management (`/WorkflowManagement`)
3. Mission Queue (`/MissionQueue`)
4. Mission History (`/MissionHistory`)
5. Mission Configuration (`/MissionConfiguration`)
6. Custom Missions (`/CustomMission`)
7. QR Code Management (`/QrCode`)
8. Map Zone Management (`/MapZone`)
9. Mobile Robot (`/MobileRobot`)
10. Area Management (`/Area`)
11. Robot Utilization Analytics (`/Analytics/RobotUtilization`)
12. Workflow Analytics (`/Analytics/WorkflowAnalytics`)
13. User Management (`/UserManagement`)
14. Role Management (`/RoleManagement`)

## How to Apply

### Option 1: Restart Application (Automatic)
1. Stop the QES-KUKA-AMR-API application
2. Start it again
3. The seeding will run automatically on startup
4. Login as Admin - all pages should now be visible

### Option 2: Manual SQL Execution
1. Connect to SQL Server
2. Run the `seed-roles-pages.sql` script
3. Restart the web application
4. Login as Admin

## Verification
After seeding, run this query to verify:
```sql
SELECT 
    r.Name AS RoleName,
    p.PageName,
    p.PagePath
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.Id
INNER JOIN [Page] p ON rp.PageId = p.Id
WHERE r.RoleCode = 'ADMIN'
ORDER BY p.PageName;
```

You should see 14 rows showing all pages assigned to Admin.

## Files Modified
1. `QES-KUKA-AMR-API/Services/Database/DatabaseInitializationService.cs`
   - Added `SeedRolesAsync()`
   - Added `SeedPagesAsync()` 
   - Added `SeedRolePermissionsAsync()`
   
2. `QES-KUKA-AMR-API/Program.cs`
   - Registered `IDatabaseInitializationService`
   - Added startup seeding logic

3. `seed-roles-pages.sql` (NEW)
   - Manual seeding script for SQL Server
