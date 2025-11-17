# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

QES KUKA AMR (Autonomous Mobile Robot) Management System - A .NET 8.0 solution for managing KUKA AMR fleet operations for Renesas at the Penang facility. The system consists of two main applications:

- **QES-KUKA-AMR-API**: Main backend API managing robot operations, analytics, workflow orchestration, and mobile robot management
- **QES-KUKA-AMR-API-Simulator**: Test simulator mimicking the external AMR control system API for development and testing

## Build and Run Commands

### Build
```bash
# Build entire solution
dotnet build QES-KUKA-AMR.sln

# Build specific project
dotnet build QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Clean build artifacts
dotnet clean QES-KUKA-AMR.sln
```

### Run
```bash
# Run main API (default port: 5109)
dotnet run --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Run API simulator (default port: 5235)
dotnet run --project QES-KUKA-AMR-API-Simulator/QES-KUKA-AMR-API-Simulator.csproj
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Update database
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Generate migration SQL script (for deployment)
dotnet ef migrations script --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj --output migration.sql

# Drop database (use with caution!)
dotnet ef database drop --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj --force

# Restore packages
dotnet restore
```

**Migration History:** The database migrations were reset in November 2025 to resolve inconsistencies. The current migration is `InitialCreate` which creates all 17 entity tables in a clean state.

Database: SQL Server with connection string in `appsettings.json` pointing to `QES_KUKA_AMR_Penang` database. The codebase includes 17 entities across multiple domains (missions, configuration, locations, user management, mobile robots, and system settings).

## Architecture Overview

### Mission Management

The system manages AMR mission execution:
- Missions are created from workflow templates (`WorkflowDiagram`) or custom mission definitions (`SavedCustomMission`)
- Mission history is tracked in `MissionHistory` with detailed status progression
- Mobile robots (`MobileRobot` entity) are managed with configuration and firmware versioning
- Robot manual pauses are tracked (`RobotManualPause`) for operational control

### Service Layer Pattern

Services follow a consistent interface-based pattern organized by domain:
- `Services/Analytics/`: Robot and workflow analytics (`RobotAnalyticsService`, `WorkflowAnalyticsService`)
- `Services/Areas/`: Area/zone management (`AreaService`)
- `Services/Login/`: External system authentication (`LoginServiceClient`)
- `Services/Missions/`: Mission operations (`JobStatusClient`, `MissionListClient`)
- `Services/MissionTypes/`, `Services/RobotTypes/`, `Services/ResumeStrategies/`, `Services/ShelfDecisionRules/`: Configuration entity services
- `Services/SavedCustomMissions/`: Custom mission management (`SavedCustomMissionService`)
- `Services/`: Log cleanup (`LogCleanupService`), background tasks (`LogCleanupHostedService`)


All services are registered as scoped dependencies except `TimeProvider` (singleton).

**Common Service Pattern:**
```csharp
public interface IEntityService
{
    Task<IEnumerable<Entity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Entity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Entity> CreateAsync(Entity entity, CancellationToken cancellationToken = default);
    Task<Entity> UpdateAsync(Entity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

### Database Context

`ApplicationDbContext` (`Data/ApplicationDbContext.cs`) manages all entities with specific configurations:
- Indexed columns for performance (e.g., MissionCode, AssignedRobotId+CompletedDate, TriggerSource+CompletedDate)
- datetime2 column types for all timestamps
- Soft delete support via query filters (e.g., `SavedCustomMission.IsDeleted`)
- Unique constraints on business keys (WorkflowCode, QrCode NodeLabel+MapCode, ZoneCode, Username, PagePath, etc.)
- User management entities with role-based permissions (`User`, `Role`, `RolePermission`, `Page`)
- System configuration via `SystemSetting` entity (key-value pairs for dynamic settings)

### External System Integration

The API integrates with an external AMR control system (simulated by QES-KUKA-AMR-API-Simulator):

**Configuration** (`appsettings.json`):
- `LoginService.LoginUrl`: Authentication endpoint with optional password hashing
- `MissionService`: Mission submission, cancellation, job/workflow queries, operation feedback, robot queries
- `QrCodeService.QrCodeListUrl`: QR code/node position data
- `MapZoneService.MapZoneListUrl`: Map zone cascade list
- `MissionListService.MissionListUrl`: Mission list queries
- `MobileRobotService.MobileRobotListUrl`: Mobile robot list
- `AmrServiceOptions`: Operation feedback and robot query URLs (alternative configuration)
- `LogCleanup.LogDirectory`: Directory path for log file management
- `RobotPositionPolling`: Interval (1s) and max attempts (500) for robot position polling
- `JobStatusPolling`: Polling interval (1s), max attempts (120), and status code mappings

**Authentication Flow:**
- Simulator uses JWT bearer authentication (see `QES-KUKA-AMR-API-Simulator/Auth/SimulatorJwtOptions.cs`)
- Main API forwards requests with appropriate headers: `language`, `accept`, `wizards`
- JWT tokens issued by login endpoint with configurable password hashing

### Logging

Uses log4net for file-based logging:
- Configuration: `log4net.config` in API project
- Log directory: `Logs/` in application base directory (created automatically)
- Console output also available via standard ASP.NET Core logging
- Log cleanup: `LogCleanupService` manages log retention based on `SystemSetting.LogRetentionMonths` (default: 1 month, max: 120 months)
- Log cleanup scans for folders in `yyyy-MM` format and deletes those older than retention period

### CORS Configuration

The API is configured with permissive CORS for development:
- Policy: `AllowAnyOrigin()`, `AllowAnyMethod()`, `AllowAnyHeader()`
- Applied globally to all endpoints
- **Important**: Review and restrict CORS policy for production deployment

### API Documentation

Swagger/OpenAPI enabled for both main API and simulator:
- Main API: `/swagger` endpoint (always enabled, not restricted to Development)
- JWT bearer authentication configured in Swagger UI
- HTTPS redirection disabled for IIS HTTP deployment

## Code Organization

### Controllers
Each controller handles a specific domain:
- Mission operations: `MissionsController`, `MissionHistoryController`, `MissionDataController`
- Configuration: `ConfigController`, `MissionTypesController`, `RobotTypesController`, `ShelfDecisionRulesController`, `ResumeStrategiesController`, `AreasController`
- Analytics: `RobotAnalyticsController`
- External system proxies: `LoginController`, `QrCodesController`, `MapZonesController`, `WorkflowsController`
- Robot operations: `OperationFeedbackController`, `RobotQueryController`, `MobileRobotController`
- Custom missions: `SavedCustomMissionsController`
- Maintenance: `LogCleanupController`

### Models
Organized by domain with DTOs for request/response:
- `Models/Missions/`: Mission-related DTOs (SubmitMissionRequest, etc.)
- `Models/Jobs/`: Job status models
- `Models/Analytics/`: Analytics DTOs
- `Models/MobileRobot/`: Mobile robot configuration and management
- `Models/Login/`: Authentication request/response models
- `Models/QrCode/`, `Models/MapZone/`, `Models/Workflow/`: External system data models
- `Models/Page/`: Page permission models
- Each configuration entity has corresponding models in dedicated folders (MissionTypes, RobotTypes, Areas, ResumeStrategies, ShelfDecisionRules, SavedCustomMissions)
- `ApiResponse.cs`: Standard API response wrapper

### Data Layer
- `Data/Entities/`: EF Core entity classes mapping to database tables
- `Data/Migrations/`: EF Core migration history
- All entities use `datetime2` for timestamps and include CreatedUtc/UpdatedUtc audit fields

## Important Implementation Notes

1. **Namespace Naming**: Root namespace is `QES_KUKA_AMR_API` (underscores, not hyphens) due to project name with dashes. This affects all using statements and fully-qualified type names.

2. **HTTP Client Usage**: Use `IHttpClientFactory` for all external HTTP calls to avoid socket exhaustion. All external service clients (`LoginServiceClient`, `JobStatusClient`, `MissionListClient`) follow this pattern. `WorkflowAnalyticsService` uses typed HttpClient with base address configured.

3. **JSON Serialization**:
   - Mission data stored as JSON in `MissionStepsJson` columns (nvarchar(max))
   - User roles stored as JSON in `RolesJson` column with `[NotMapped]` property for object access
   - Custom converters handle edge cases with empty strings and nullable numbers

4. **TimeProvider Abstraction**: Use injected `TimeProvider` instead of `DateTime.UtcNow` for testability. Registered as singleton in `Program.cs`.

5. **Mobile Robot Management**:
   - `MobileRobot` entity tracks robot configuration with `SendConfigTime` and firmware with `SendFirmwareTime`
   - RobotId limited to 100 characters
   - Includes audit fields: `CreateTime`, `LastUpdateTime`

6. **Composite Indices**: Critical for query performance:
   - `MissionHistory`: `(AssignedRobotId, CompletedDate)`, `(TriggerSource, CompletedDate)`
   - `RobotManualPause`: `(RobotId, PauseStartUtc)`, `(MissionCode, PauseStartUtc)`
   - `SavedCustomMission`: `(CreatedBy, IsDeleted)`
   - `QrCode`: `(NodeLabel, MapCode)` unique, `MapCode` non-unique

7. **External System Headers**: Requests to external AMR system require custom headers: `language: "en"`, `accept: "*/*"`, `wizards: "FRONT_END"`

8. **Log Cleanup**:
   - `LogCleanupService` manages log file retention using `SystemSetting.LogRetentionMonths` (default: 1, max: 120)
   - Scans for folders matching `yyyy-MM` format in configured log directory
   - Can be triggered manually via `LogCleanupController` or scheduled as background service

9. **User & Role Management**:
   - `User.RolesJson` stores roles as JSON array; use `Roles` property for object access
   - `RolePermission` links roles to pages via composite unique index on `(RoleId, PageId)`
   - Usernames are unique across the system

10. **CORS Policy**: Current policy allows all origins - **MUST** be restricted for production deployment

11. **Database Migrations**: After migration reset (November 2025), the codebase has a single clean InitialCreate migration. Always review generated migration code before applying to ensure proper index and constraint creation.

## Development Workflow

When working with this codebase:
1. **Database schema changes**: Create migration with `dotnet ef migrations add`, review generated code for proper indices and constraints, test locally, then apply to production database
2. **External API integration**: Always use the simulator (`QES-KUKA-AMR-API-Simulator`) for development/testing before connecting to real AMR system
3. **New services**: Follow existing interface-based pattern with DI registration in `Program.cs`. Use scoped lifetime unless specific needs require singleton/transient
4. **Configuration options**: Create Options class in `Options/` folder, add configuration section to `appsettings.json`, register with `Configure<TOptions>()` in `Program.cs`
5. **Controllers**: Inherit from `ControllerBase`, use dependency injection for services, return consistent response types (use `ApiResponse` wrapper where applicable)
6. **Log cleanup testing**: Use `LogCleanupController` to manually trigger cleanup; verify `SystemSetting.LogRetentionMonths` setting exists
7. **CORS**: Update CORS policy before production deployment to restrict allowed origins

## Key Database Entities

The codebase includes 17 main entities organized across multiple domains:

**Mission & Workflow Entities:**
- `WorkflowDiagram`: Workflow templates with unique WorkflowCode and configurations (CreateTime, UpdateTime)
- `MissionHistory`: Archive of completed missions with status progression tracking (CreatedDate, ProcessedDate, SubmittedToAmrDate, CompletedDate)
  - Indices: MissionCode, SavedMissionId, (AssignedRobotId, CompletedDate), (TriggerSource, CompletedDate)

**Robot & Operation Entities:**
- `MobileRobot`: Robot definitions with configuration and firmware tracking
  - Fields: RobotId (max 100 chars), CreateTime, LastUpdateTime, SendConfigTime, SendFirmwareTime
- `RobotManualPause`: Manual pause tracking for robots
  - Indices: (RobotId, PauseStartUtc), (MissionCode, PauseStartUtc)

**Configuration Entities:**
All configuration entities share common pattern: DisplayName, ActualValue (unique), Description, CreatedUtc, UpdatedUtc
- `MissionType`: Types of missions (e.g., RACK_MOVE, DELIVERY)
- `RobotType`: Robot classifications (e.g., LIFT, LATENT)
- `ShelfDecisionRule`: Logic for robot selection and assignment
- `ResumeStrategy`: Recovery strategies for failed missions
- `Area`: Warehouse area/zone definitions

**Location & Mapping Entities:**
- `QrCode`: Position markers with composite unique index (NodeLabel, MapCode)
  - Additional index on MapCode for queries
  - Fields: CreateTime, LastUpdateTime
- `MapZone`: Zone definitions with unique ZoneCode
  - Fields: CreateTime, LastUpdateTime, BeginTime, EndTime, MapCode index

**Custom Mission Entities:**
- `SavedCustomMission`: User-defined mission templates
  - Soft delete supported via IsDeleted with query filter
  - Fields: MissionName (indexed), MissionStepsJson (nvarchar(max)), CreatedBy (indexed with IsDeleted)
  - Indices: MissionName, (CreatedBy, IsDeleted)

**User Management Entities:**
- `User`: User accounts with unique Username
  - Fields: Username, Nickname, IsSuperAdmin, RolesJson (nvarchar(max) storing JSON array)
  - [NotMapped] Roles property for object access
  - Audit fields: CreateTime, CreateBy, CreateApp, LastUpdateTime, LastUpdateBy, LastUpdateApp
- `Role`: Role definitions with unique RoleCode
  - Fields: Name, RoleCode (unique index), IsProtected
- `RolePermission`: Links roles to pages
  - Composite unique index on (RoleId, PageId)
- `Page`: Page definitions for permission system
  - Fields: PagePath (unique index), CreatedAt

**System Configuration Entities:**
- `SystemSetting`: Dynamic key-value configuration storage
  - Fields: Key (unique, max 100 chars), Value (max 255 chars), LastUpdated (datetime2)
  - Example usage: LogRetentionMonths for log cleanup configuration

**Important Notes:**
- All datetime fields use `datetime2` SQL Server column type for precision
- Most entities include audit fields (various naming: CreatedUtc/CreateTime, UpdatedUtc/LastUpdateTime)
- Soft delete pattern: Query filters automatically exclude deleted records (e.g., SavedCustomMission.IsDeleted)
- JSON storage: Mission steps, user roles stored as JSON in nvarchar(max) columns with [NotMapped] properties for object access


