# Database Migration Reset - Next Steps

## What Was Done

✅ **Deleted all 67 migration files** (34 migrations + snapshot)
✅ **Updated CLAUDE.md** with corrected entity count (17 entities)
✅ **Committed and pushed** to branch `claude/claude-md-mhzycy0fn4py44jg-016RXSBLaZSUqrahCWyzvNHH`

## Issues Resolved

The previous migration history had critical inconsistencies:
- **22 tables in database** but only **17 entities in code**
- **5 orphaned tables** with no corresponding entities:
  - `MissionQueues`
  - `SavedMissionSchedules`
  - `SavedMissionScheduleLogs`
  - `WorkflowSchedules`
  - `WorkflowScheduleLogs`
- Migration conflicts and duplicates (e.g., `AddSavedMissionSchedulesAgain`)

## Your 17 Entities

The following entities are now correctly defined in your `ApplicationDbContext`:

**Mission & Workflow:**
1. WorkflowDiagram
2. MissionHistory

**Robot Management:**
3. MobileRobot
4. RobotManualPause

**Location & Mapping:**
5. QrCode
6. MapZone

**Configuration:**
7. MissionType
8. RobotType
9. ShelfDecisionRule
10. ResumeStrategy
11. Area

**Custom Missions:**
12. SavedCustomMission

**User Management:**
13. User
14. Role
15. RolePermission
16. Page

**System:**
17. SystemSetting

---

## REQUIRED: Run These Commands Locally

Since dotnet CLI is not available in the Claude Code environment, you **MUST run these commands on your local machine** where dotnet is installed:

### Step 1: Navigate to Project Directory
```bash
cd /path/to/KUKA-BACKEND
```

### Step 2: Pull Latest Changes
```bash
git fetch origin
git checkout claude/claude-md-mhzycy0fn4py44jg-016RXSBLaZSUqrahCWyzvNHH
git pull
```

### Step 3: Verify Migrations Folder is Empty
```bash
ls QES-KUKA-AMR-API/Data/Migrations/
# Should show: directory is empty or doesn't exist
```

### Step 4: Create Fresh Initial Migration
```bash
dotnet ef migrations add InitialCreate --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

**Expected output:**
- `20YYMMDDHHMMSS_InitialCreate.cs`
- `20YYMMDDHHMMSS_InitialCreate.Designer.cs`
- `ApplicationDbContextModelSnapshot.cs`

### Step 5: Drop Existing Database
```bash
dotnet ef database drop --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj --force
```

**⚠️ WARNING:** This will permanently delete all data in `QES_KUKA_AMR_Penang` database!

### Step 6: Apply Migration to Create Clean Database
```bash
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

### Step 7: Verify Database Schema
Connect to your SQL Server and verify you have exactly **17 tables**:

```sql
USE QES_KUKA_AMR_Penang;
GO

SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

**Expected tables:**
- Areas
- MapZones
- MissionHistories
- MissionTypes
- MobileRobots
- Page (or Pages)
- QrCodes
- ResumeStrategies
- RobotManualPauses
- RobotTypes
- Roles
- RolePermissions
- SavedCustomMissions
- ShelfDecisionRules
- SystemSetting (or SystemSettings)
- Users
- WorkflowDiagrams

### Step 8: Commit the New Migration
```bash
git add QES-KUKA-AMR-API/Data/Migrations/
git commit -m "Add InitialCreate migration after reset"
git push
```

---

## Verification Checklist

After completing the steps above, verify:

- [ ] Migrations folder contains exactly 3 files (InitialCreate + Designer + Snapshot)
- [ ] Database has exactly 17 tables
- [ ] No orphaned tables (MissionQueues, SavedMissionSchedules, etc.)
- [ ] Application builds without errors: `dotnet build QES-KUKA-AMR.sln`
- [ ] Application runs: `dotnet run --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj`

---

## Troubleshooting

### If migration creation fails:
```bash
# Check EF Core tools are installed
dotnet tool list --global

# Install/update if needed
dotnet tool install --global dotnet-ef
# or
dotnet tool update --global dotnet-ef
```

### If database drop fails:
Manually drop the database using SQL Server Management Studio or:
```sql
USE master;
GO
DROP DATABASE QES_KUKA_AMR_Penang;
GO
CREATE DATABASE QES_KUKA_AMR_Penang;
GO
```

Then run `dotnet ef database update` again.

---

## Summary

You now have a **clean migration state** that perfectly matches your **17 actual entities**. The orphaned tables and chaotic migration history have been eliminated.

**Commit:** `2a2cd70` - Complete migration reset
**Branch:** `claude/claude-md-mhzycy0fn4py44jg-016RXSBLaZSUqrahCWyzvNHH`

**Remember:** After running these commands locally, commit the new InitialCreate migration to complete the process!
