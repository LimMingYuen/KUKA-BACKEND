# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

QES KUKA AMR (Autonomous Mobile Robot) Management System - A .NET 8.0 solution for managing KUKA AMR fleet operations for Renesas at the Penang facility. The system consists of three main applications:

- **QES-KUKA-AMR-API**: Main backend API managing mission queues, robot operations, analytics, and workflow orchestration
- **QES-KUKA-AMR-WEB**: ASP.NET Razor Pages frontend for user interaction
- **QES-KUKA-AMR-API-Simulator**: Test simulator mimicking the external AMR control system API

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

# Run web frontend
dotnet run --project QES-KUKA-AMR-WEB/QES-KUKA-AMR-WEB.csproj

# Run API simulator (port: 5235)
dotnet run --project QES-KUKA-AMR-API-Simulator/QES-KUKA-AMR-API-Simulator.csproj
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests in specific project
dotnet test tests/QES-KUKA-AMR-API.Tests/QES-KUKA-AMR-API.Tests.csproj

# Run single test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

Test framework: xUnit with FluentAssertions, Microsoft.AspNetCore.Mvc.Testing for integration tests, and Entity Framework Core InMemory for database mocking.

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Update database
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Generate migration SQL script (for deployment)
dotnet ef migrations script --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj --output migration.sql

# Restore packages
dotnet restore
```

Database: SQL Server with connection string in `appsettings.json` pointing to `QES_KUKA_AMR_Penang` database. The codebase includes 16 entities across multiple domains (missions, configuration, locations, scheduling).

## Architecture Overview

### Mission Queue System (Core Feature)

The application implements a sophisticated mission queue management system with concurrency control:

**Key Components:**
- **QueueService** (`Services/QueueService.cs`): Singleton service managing mission queue with static semaphores for global concurrency control (`_globalSemaphore`, `_dbLock`)
- **MissionQueue entity** (`Data/Entities/MissionQueue.cs`): Tracks missions with statuses: Queued, Processing, Completed, Failed, Cancelled. Additional tracking includes robot info (AssignedRobotId, RobotNodeCode, RobotStatusCode, RobotBatteryLevel), manual waypoint handling (IsWaitingForManualResume, ManualWaypointsJson), and trigger source (Manual, Scheduled, Workflow, API, Direct)
- **Background Services**:
  - `QueueProcessorBackgroundService`: Processes queued missions based on priority and global concurrency limits
  - `MissionSubmitterBackgroundService`: Submits missions to external AMR system
  - `JobStatusPollerBackgroundService`: Polls job status from external system
  - `SavedMissionSchedulerBackgroundService`: Handles scheduled mission execution

**Critical Implementation Details:**
- Global concurrency is controlled via `MissionQueueSettings.GlobalConcurrencyLimit` (default: 2 concurrent missions)
- **QueueService.InitializeAsync()** MUST be called on startup to sync semaphore state with database (prevents SemaphoreFullException after app restart)
- Semaphores are static and shared across all service instances to maintain global state
- Priority-based queue processing with lower priority numbers processed first
- Mission statuses flow: `Queued → Processing → Completed/Failed/Cancelled`
- Missions track `SubmittedToAmr` flag separately from status to coordinate submission timing

### Service Layer Pattern

Services follow a consistent interface-based pattern organized by domain:
- `Services/Analytics/`: Robot and workflow analytics (`RobotAnalyticsService`, `WorkflowAnalyticsService`)
- `Services/Areas/`: Area/zone management (`AreaService`)
- `Services/Login/`: External system authentication (`LoginServiceClient`)
- `Services/Missions/`: Mission operations (`JobStatusClient`)
- `Services/MissionTypes/`, `Services/RobotTypes/`, `Services/ResumeStrategies/`, `Services/ShelfDecisionRules/`: Configuration entity services
- `Services/SavedCustomMissions/`: Custom mission and scheduling services (`SavedCustomMissionService`, `SavedMissionScheduleService`)
- `Services/Database/`: Database initialization (`DatabaseInitializationService`)

All services are registered as scoped dependencies except `QueueService` (singleton) and `TimeProvider` (singleton).

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
- Indexed columns for performance (e.g., MissionCode, Status+Priority+CreatedDate composite index)
- datetime2 column types for all timestamps
- Soft delete support via query filters (e.g., `SavedCustomMission.IsDeleted`)
- Unique constraints on business keys (WorkflowCode, QrCode NodeLabel+MapCode, ZoneCode, etc.)

### External System Integration

The API integrates with an external AMR control system (simulated by QES-KUKA-AMR-API-Simulator):

**Configuration** (`appsettings.json`):
- `LoginService.LoginUrl`: Authentication endpoint
- `MissionService.SubmitMissionUrl`: Mission submission
- `MissionService.JobQueryUrl`: Job status queries
- `MissionService.RobotQueryUrl`: Robot state queries
- `QrCodeService`, `MapZoneService`: Map/location data endpoints

**Authentication Flow:**
- Simulator uses JWT bearer authentication (see `QES-KUKA-AMR-API-Simulator/Auth/SimulatorJwtOptions.cs`)
- Main API forwards requests with appropriate headers: `language`, `accept`, `wizards`
- JWT tokens issued by login endpoint with configurable password hashing

### Logging

Uses log4net for file-based logging:
- Configuration: `log4net.config` in API project
- Log directory: `Logs/` in application base directory (created automatically)
- Console output also available via standard ASP.NET Core logging

### API Documentation

Swagger/OpenAPI enabled for both main API and simulator:
- Main API: `/swagger` endpoint (always enabled, not restricted to Development)
- JWT bearer authentication configured in Swagger UI
- HTTPS redirection disabled for IIS HTTP deployment

## Code Organization

### Controllers
Each controller handles a specific domain (19 controllers total):
- Mission operations: `MissionsController`, `MissionQueueController`, `MissionHistoryController`
- Configuration: `ConfigController`, `MissionTypesController`, `RobotTypesController`, `ShelfDecisionRulesController`, `ResumeStrategiesController`, `AreasController`
- Analytics: `RobotAnalyticsController`, `WorkflowAnalyticsController`
- External system proxies: `LoginController`, `QrCodesController`, `MapZonesController`, `WorkflowsController`
- Robot interaction: `OperationFeedbackController`, `RobotQueryController`
- Custom missions: `SavedCustomMissionsController`, `SavedMissionSchedulesController`
- Database operations: `DatabaseController`

### Models
Organized by domain with DTOs for request/response:
- `Models/Missions/`: Mission-related DTOs (SubmitMissionRequest, EnqueueRequest, etc.)
- `Models/Jobs/`: Job status models
- `Models/Analytics/`: Analytics DTOs
- Each configuration entity has corresponding models in dedicated folders

### Data Layer
- `Data/Entities/`: EF Core entity classes mapping to database tables
- `Data/Migrations/`: EF Core migration history
- All entities use `datetime2` for timestamps and include CreatedUtc/UpdatedUtc audit fields

## Important Implementation Notes

1. **Namespace Naming**: Root namespace is `QES_KUKA_AMR_API` (underscores, not hyphens) due to project name with dashes
2. **Mission Queue Initialization**: Always ensure `QueueService.InitializeAsync()` is called during application startup in `Program.cs` (currently via background service). This syncs static semaphore state with database to prevent `SemaphoreFullException` after app restarts
3. **Concurrency Control**: Do not modify semaphore logic without understanding global state implications across app restarts. The `_globalSemaphore` and `_dbLock` are static fields shared across all service instances
4. **HTTP Client Usage**: Use `IHttpClientFactory` for all external HTTP calls to avoid socket exhaustion. All external service clients (`LoginServiceClient`, `JobStatusClient`) follow this pattern
5. **JSON Serialization**: Mission data stored as JSON in `MissionDataJson` and `MissionStepsJson` columns (nvarchar(max)). Custom converters handle edge cases with empty strings and nullable numbers
6. **TimeProvider Abstraction**: Use injected `TimeProvider` instead of `DateTime.UtcNow` for testability. Registered as singleton in `Program.cs`
7. **Session Management**: Web project uses distributed memory cache for session state (30-minute timeout)
8. **Manual Waypoints**: Missions can pause at manual waypoints requiring user confirmation. Fields: `IsWaitingForManualResume`, `CurrentManualWaypointPosition`, `ManualWaypointsJson`, `VisitedManualWaypointsJson`
9. **Composite Indices**: Critical for query performance - `MissionQueue` has composite index on `(Status, Priority, CreatedDate)` for efficient queue processing
10. **External System Headers**: Requests to external AMR system require custom headers: `language: "en"`, `accept: "*/*"`, `wizards: "FRONT_END"`

## Development Workflow

When working with this codebase:
1. For database schema changes: Create migration, review generated code, test locally, then apply to production database
2. For mission queue changes: Understand the semaphore lifecycle and test app restart scenarios
3. For external API integration: Use simulator for development/testing before connecting to real AMR system
4. For background services: Test cancellation token handling and graceful shutdown
5. For new services: Follow existing interface-based pattern with DI registration in `Program.cs`

## Key Database Entities

The codebase includes 16 main entities:

**Mission Entities:**
- `WorkflowDiagram`: Workflow templates with codes and configurations
- `MissionQueue`: Active mission queue with status tracking
- `MissionHistory`: Archive of completed missions

**Configuration Entities:**
- `MissionType`: Types of missions (e.g., RACK_MOVE, DELIVERY)
- `RobotType`: Robot classifications (e.g., LIFT, LATENT)
- `ShelfDecisionRule`: Logic for robot selection and assignment
- `ResumeStrategy`: Recovery strategies for failed missions
- `Area`: Warehouse area definitions
- `RobotManualPause`: Manual pause tracking for robots

**Location Entities:**
- `QrCode`: Position markers with unique (NodeLabel, MapCode) combinations
- `MapZone`: Zone definitions with unique ZoneCode

**Custom Mission Entities:**
- `SavedCustomMission`: User-defined mission templates (soft delete supported via IsDeleted)
- `SavedMissionSchedule`: Scheduled mission triggers (cron expressions or one-time schedules)
- `SavedMissionScheduleLog`: Execution history for scheduled missions

All entities include audit fields (`CreatedUtc`, `UpdatedUtc`) and use `datetime2` SQL Server column types.

## Mission Queue Processing Flow

Understanding the complete mission lifecycle is critical for debugging and enhancements:

1. **Enqueue Phase** - Mission submission via `MissionsController.SubmitMissionAsync()`
   - Validates mission data and configuration
   - Creates `MissionQueue` record with `Status = Queued`
   - Sets `SubmittedToAmr = false`
   - Returns immediately to client

2. **Processing Phase** - `QueueProcessorBackgroundService` (5-second interval)
   - Queries missions with `Status = Queued` ordered by `(Priority ASC, CreatedDate ASC)`
   - Attempts to acquire `_globalSemaphore` slot (max 2 concurrent by default)
   - If acquired: Updates `Status = Processing`, sets `ProcessedDate`
   - If not acquired: Mission remains `Queued`, retries next interval

3. **Submission Phase** - `MissionSubmitterBackgroundService` (5-second interval)
   - Queries missions with `Status = Processing AND SubmittedToAmr = false`
   - POSTs mission to external AMR system via `MissionService.SubmitMissionUrl`
   - On success: Sets `SubmittedToAmr = true`, `SubmittedToAmrDate = now`
   - On failure: Logs error, mission remains in Processing state for retry

4. **Status Polling Phase** - `JobStatusPollerBackgroundService` (10-second interval)
   - Queries status from AMR via `MissionService.JobQueryUrl` for all Processing missions
   - Updates robot tracking fields: `AssignedRobotId`, `RobotNodeCode`, `RobotStatusCode`, `RobotBatteryLevel`, `LastRobotQueryTime`
   - Checks for terminal states (Complete=5, Cancelled=31, ManualComplete=32)
   - On completion: Updates `Status = Completed/Failed`, sets `CompletedDate`, releases `_globalSemaphore` slot

5. **Scheduled Missions** - `SavedMissionSchedulerBackgroundService` (60-second interval)
   - Evaluates cron expressions and one-time schedules
   - Triggers missions via `QueueService.EnqueueMissionAsync()` when schedule fires
   - Creates `SavedMissionScheduleLog` records for audit trail

**Important Notes:**
- The separation of Processing and Submission phases allows for retry logic without losing the concurrency slot
- Semaphore slots are held from Processing phase through completion
- Robot status codes (Created=0, Executing=2, Waiting=3, Cancelling=4, Complete=5, Cancelled=31, ManualComplete=32, Warning=50, StartupError=99) are configured in `appsettings.json` under `JobStatusPolling.StatusCodes`
