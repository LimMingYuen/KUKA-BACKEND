# Queue System Design Document

## Document Information

| Field | Value |
|-------|-------|
| Version | 1.0 (Draft) |
| Created | November 2025 |
| Status | Design Review |
| Author | AI Assistant |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Overview](#2-system-overview)
3. [Area-Based Queue System](#3-area-based-queue-system)
4. [Job Optimization System](#4-job-optimization-system)
5. [Distance Calculation](#5-distance-calculation)
6. [Robot Availability Integration](#6-robot-availability-integration)
7. [Core Components](#7-core-components)
8. [Database Design](#8-database-design)
9. [API Endpoints](#9-api-endpoints)
10. [Risks and Concerns](#10-risks-and-concerns)
11. [Open Questions](#11-open-questions)
12. [Implementation Phases](#12-implementation-phases)

---

## 1. Executive Summary

### Problem Statement

The current system submits missions directly to the external AMR system without traffic control. During peak periods, this causes:

- Multiple robots competing for the same paths
- Inefficient robot utilization (robots traveling empty across maps)
- No optimization for cross-map workflows
- No priority-based task execution

### Proposed Solution

Implement a queue system with:

1. **Area-based queues** - Each area code has its own queue with independent priority management
2. **Job optimization** - For specific workflows (identified by `OrgId = "JOBOPTIMIZE"`), optimize task execution order based on robot location
3. **Distance-based selection** - When multiple tasks are available, execute the nearest one first
4. **Robot availability checking** - Query robot status before task submission

### Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Priority Model | Strict Priority | Higher priority missions always execute first |
| Battery Consideration | Not included | Simplify initial implementation |
| Missing Coordinates | Fail the task | Ensure data integrity |
| Robot Failures | Not handled | Phase 1 simplification |

---

## 2. System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Frontend (Angular)                          │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      Backend API (.NET 8.0)                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │
│  │ Mission         │  │ Queue           │  │ Job Optimization    │  │
│  │ Controller      │  │ Service         │  │ Service             │  │
│  └────────┬────────┘  └────────┬────────┘  └──────────┬──────────┘  │
│           │                    │                      │             │
│           ▼                    ▼                      ▼             │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │              Queue Processor (Background Service)               ││
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐    ││
│  │  │ Area A   │  │ Area B   │  │ Area C   │  │ Optimization │    ││
│  │  │ Queue    │  │ Queue    │  │ Queue    │  │ Engine       │    ││
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────────┘    ││
│  └─────────────────────────┬───────────────────────────────────────┘│
└────────────────────────────┼────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    External AMR System                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                 │
│  │ Robot Query │  │ Submit      │  │ Job Query   │                 │
│  │ API         │  │ Mission API │  │ API         │                 │
│  └─────────────┘  └─────────────┘  └─────────────┘                 │
└─────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
1. User triggers mission (Frontend)
        │
        ▼
2. Backend validates request
        │
        ▼
3. Check if Job Optimization required (OrgId == "JOBOPTIMIZE")
        │
        ├─── YES ──► 4a. Queue to appropriate area with optimization flag
        │
        └─── NO ───► 4b. Queue to appropriate area (normal processing)
        │
        ▼
5. Queue Processor picks up task (by priority)
        │
        ▼
6. Query robot availability
        │
        ├─── Available ──► 7a. Apply optimization (if flagged) → Submit to AMR
        │
        └─── Busy ────────► 7b. Return to queue, try next task
```

---

## 3. Area-Based Queue System

### Concept

Each **area code** maintains its own independent queue. This allows:

- Parallel processing of tasks in different areas
- Area-specific priority management
- Isolation of traffic per area

### Queue Structure

```
Area Code: "AREA_001"
┌────────────────────────────────────────────┐
│ Priority 1 (Highest)                       │
│   Task A1 → Task A2 → Task A3              │
├────────────────────────────────────────────┤
│ Priority 2                                 │
│   Task B1 → Task B2                        │
├────────────────────────────────────────────┤
│ Priority 3 (Lowest)                        │
│   Task C1 → Task C2 → Task C3 → Task C4    │
└────────────────────────────────────────────┘

Area Code: "AREA_002"
┌────────────────────────────────────────────┐
│ Priority 1 (Highest)                       │
│   Task D1                                  │
├────────────────────────────────────────────┤
│ Priority 2                                 │
│   (empty)                                  │
├────────────────────────────────────────────┤
│ Priority 3 (Lowest)                        │
│   Task E1 → Task E2                        │
└────────────────────────────────────────────┘
```

### Priority Levels

| Priority | Value | Description |
|----------|-------|-------------|
| Critical | 1 | Emergency/Safety tasks |
| High | 2 | Time-sensitive operations |
| Normal | 3 | Standard operations |
| Low | 4 | Background tasks |

### Queue Processing Rules

1. **Strict Priority**: Tasks with priority 1 always execute before priority 2, etc.
2. **FIFO within Priority**: Tasks at the same priority level process in order of arrival
3. **Robot Exclusivity**: A robot can only execute one task at a time
4. **Area Parallelism**: Different areas can process simultaneously if robots are available

---

## 4. Job Optimization System

### Identification

Job optimization is triggered when a SavedCustomMission has:

```csharp
OrgId == "JOBOPTIMIZE"
```

### What is a Cross-Map Workflow?

A workflow that moves a robot from **Map A** to **Map B**:

```
Workflow Example: "TRANSFER_TO_WAREHOUSE"
┌─────────────┐                    ┌─────────────┐
│   Map A     │                    │   Map B     │
│  (Factory)  │  ═══════════════►  │ (Warehouse) │
│             │                    │             │
│  Node: 101  │                    │  Node: 201  │
└─────────────┘                    └─────────────┘
     START                              END
```

### The Optimization Problem

**Scenario**: Robot R1 is in Map A, executes workflow to Map B

```
Time T0: Robot R1 at Map A, Node 101
Time T1: Workflow triggered (A → B)
Time T2: Robot R1 arrives at Map B, Node 201

During T1-T2, new tasks are queued:
  - Task X: Requires R1 at Map A, Node 105
  - Task Y: Requires R1 at Map B, Node 203
```

**Without Optimization**:
```
T3: Execute Task X (R1 must travel B → A)
T4: Execute Task Y (R1 must travel A → B again)
Result: Unnecessary round trip
```

**With Optimization**:
```
T3: Execute Task Y (R1 already at Map B) ✓
T4: Execute Task X (R1 travels B → A once)
Result: One less map crossing
```

### Optimization Algorithm

```python
def select_next_task(robot, queued_tasks):
    """
    Select the optimal task for a robot based on current location.

    Args:
        robot: Current robot state (mapCode, nodeCode, x, y)
        queued_tasks: List of pending tasks for this robot

    Returns:
        Optimal task to execute
    """

    # Group tasks by map code
    same_map_tasks = [t for t in queued_tasks if t.mapCode == robot.mapCode]
    different_map_tasks = [t for t in queued_tasks if t.mapCode != robot.mapCode]

    # Prioritize tasks in current map
    if same_map_tasks:
        # Find task with minimum distance to robot
        return min(same_map_tasks, key=lambda t: calculate_distance(robot, t))

    # If no tasks in current map, select from other maps
    if different_map_tasks:
        return min(different_map_tasks, key=lambda t: calculate_distance(robot, t))

    return None
```

### Multiple Tasks in Target Map

When the robot arrives at Map B and multiple tasks are queued for Map B:

```
Robot R1 at Map B, Node 201 (x=1000, y=2000)

Queued Tasks in Map B:
  - Task Y1: Node 203 (x=1200, y=2100) → Distance: 224mm
  - Task Y2: Node 205 (x=3000, y=4000) → Distance: 2828mm
  - Task Y3: Node 202 (x=1100, y=1900) → Distance: 141mm ✓ SELECTED
```

**Selection**: Task Y3 is selected because it has the shortest distance to the robot's current position.

---

## 5. Distance Calculation

### Formula

Using Euclidean distance between robot position and target node:

```
Distance = √[(x₂ - x₁)² + (y₂ - y₁)²]
```

Where:
- (x₁, y₁) = Robot current position (from robot query API)
- (x₂, y₂) = Target node position (from QrCode entity)

### Implementation

```csharp
public class DistanceCalculator
{
    public double Calculate(RobotPosition robot, NodePosition target)
    {
        // Validate coordinates exist
        if (!robot.X.HasValue || !robot.Y.HasValue)
            throw new InvalidOperationException("Robot coordinates unavailable");

        if (!target.X.HasValue || !target.Y.HasValue)
            throw new InvalidOperationException("Target node coordinates unavailable");

        var deltaX = target.X.Value - robot.X.Value;
        var deltaY = target.Y.Value - robot.Y.Value;

        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
```

### Coordinate Source

**Robot Position**: From Robot Query API response

```json
{
  "robotId": "13",
  "nodeCode": "46",
  "x": "3000.0",
  "y": "15000.0",
  "mapCode": "M001"
}
```

**Node Position**: From QrCode entity in database

```sql
SELECT NodeLabel, XCoordinate, YCoordinate, MapCode
FROM QrCodes
WHERE NodeLabel = '203' AND MapCode = 'M001'
```

### Validation Requirements

**CRITICAL**: Tasks will be **rejected** if coordinates are missing.

```csharp
// Pre-queue validation
public async Task<bool> ValidateTaskCoordinates(QueueItem task)
{
    // Get target node
    var node = await _dbContext.QrCodes
        .FirstOrDefaultAsync(q =>
            q.NodeLabel == task.TargetNodeCode &&
            q.MapCode == task.MapCode);

    if (node == null)
        throw new QueueValidationException($"Node {task.TargetNodeCode} not found in map {task.MapCode}");

    if (!node.XCoordinate.HasValue || !node.YCoordinate.HasValue)
        throw new QueueValidationException($"Node {task.TargetNodeCode} missing coordinates");

    return true;
}
```

---

## 6. Robot Availability Integration

### Robot Status Codes

| Status | Code | Available? | Description |
|--------|------|------------|-------------|
| Departure | 1 | No | Robot is departing |
| Offline | 2 | No | Robot is offline |
| **Idle** | **3** | **Yes** | Robot is available |
| Executing | 4 | No | Robot is executing a task |
| Charging | 5 | No | Robot is charging |
| Updating | 6 | No | Robot is updating |
| Abnormal | 7 | No | Robot has an error |

### Occupy Status

| Status | Code | Description |
|--------|------|-------------|
| **Idle** | **0** | Robot can accept tasks |
| Occupied | 1 | Robot is occupied |

### Availability Check Logic

```csharp
public bool IsRobotAvailable(RobotDataDto robot)
{
    return robot.Status == 3          // Idle
        && robot.OccupyStatus == 0;   // Not occupied
}
```

### Pre-Submission Workflow

```
1. Dequeue task from area queue
        │
        ▼
2. Identify required robot(s) from task
        │
        ▼
3. Query robot status via API
        │
        ├─── Available ──► 4a. Submit task to AMR
        │
        └─── Unavailable ► 4b. Return task to queue front
                                (try next task in queue)
```

### Robot Query Request

```json
{
  "robotId": "13",
  "robotType": "KMP600I",
  "mapCode": "M001",
  "floorNumber": "A001"
}
```

### Handling Multiple Compatible Robots

When a task specifies multiple robot IDs:

```csharp
public async Task<string?> FindAvailableRobot(List<string> robotIds)
{
    foreach (var robotId in robotIds)
    {
        var robot = await QueryRobotStatus(robotId);
        if (IsRobotAvailable(robot))
            return robotId;
    }
    return null; // No available robot
}
```

---

## 7. Core Components

### Service Interfaces

```csharp
// Queue Management
public interface IQueueService
{
    Task<int> EnqueueAsync(QueueItemRequest request, CancellationToken ct);
    Task<QueueItem?> DequeueAsync(string areaCode, CancellationToken ct);
    Task ReturnToQueueAsync(int queueItemId, CancellationToken ct);
    Task<List<QueueItem>> GetPendingAsync(string areaCode, CancellationToken ct);
    Task<QueueStatistics> GetStatisticsAsync(CancellationToken ct);
}

// Job Optimization
public interface IJobOptimizationService
{
    Task<QueueItem?> SelectOptimalTask(string robotId, List<QueueItem> candidates, CancellationToken ct);
    Task<double> CalculateDistance(string robotId, QueueItem task, CancellationToken ct);
    Task<RobotPosition> GetRobotPosition(string robotId, CancellationToken ct);
}

// Robot Availability
public interface IRobotAvailabilityService
{
    Task<bool> IsAvailableAsync(string robotId, CancellationToken ct);
    Task<string?> FindAvailableRobotAsync(List<string> robotIds, CancellationToken ct);
    Task<RobotDataDto> QueryRobotAsync(string robotId, CancellationToken ct);
}
```

### Background Service

```csharp
public class QueueProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueueProcessorBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllQueuesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queues");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    private async Task ProcessAllQueuesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();
        var areaCodes = await GetActiveAreaCodesAsync(scope, ct);

        // Process each area queue in parallel
        var tasks = areaCodes.Select(area => ProcessAreaQueueAsync(scope, area, ct));
        await Task.WhenAll(tasks);
    }
}
```

---

## 8. Database Design

### New Entities

#### QueueItem

```csharp
public class QueueItem
{
    public int Id { get; set; }

    // Queue identification
    public string AreaCode { get; set; }        // Area this task belongs to
    public int Priority { get; set; }           // 1=Critical, 2=High, 3=Normal, 4=Low
    public int QueuePosition { get; set; }      // Position within priority level

    // Task details
    public int? SavedMissionId { get; set; }    // Source mission (nullable for direct)
    public string MissionCode { get; set; }     // Generated mission code
    public string RequestId { get; set; }       // Generated request ID
    public string MissionDataJson { get; set; } // Serialized mission data

    // Robot assignment
    public string RobotIds { get; set; }        // Comma-separated compatible robots
    public string? AssignedRobotId { get; set; } // Robot selected for execution

    // Location info
    public string MapCode { get; set; }         // Target map code
    public string TargetNodeCode { get; set; }  // Target node in the map
    public double? TargetX { get; set; }        // Cached target X coordinate
    public double? TargetY { get; set; }        // Cached target Y coordinate

    // Optimization
    public bool RequiresOptimization { get; set; } // OrgId == "JOBOPTIMIZE"
    public string OrgId { get; set; }           // Original OrgId value

    // Status tracking
    public QueueItemStatus Status { get; set; } // Pending, Processing, Submitted, etc.
    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessingStartedUtc { get; set; }
    public DateTime? SubmittedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    // Audit
    public string CreatedBy { get; set; }
}

public enum QueueItemStatus
{
    Pending = 0,
    Processing = 1,
    Submitted = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
```

#### AreaQueueConfiguration

```csharp
public class AreaQueueConfiguration
{
    public int Id { get; set; }
    public string AreaCode { get; set; }        // Unique area identifier
    public string DisplayName { get; set; }     // User-friendly name
    public bool IsEnabled { get; set; }         // Queue active/inactive
    public int MaxConcurrentTasks { get; set; } // Max tasks processing simultaneously
    public int MaxQueueDepth { get; set; }      // Maximum items in queue
    public int DefaultPriority { get; set; }    // Default priority for this area
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
```

### Database Indices

```csharp
// QueueItem indices
entity.HasIndex(e => new { e.AreaCode, e.Status, e.Priority, e.QueuePosition })
    .HasDatabaseName("IX_QueueItem_AreaProcessing");

entity.HasIndex(e => e.MissionCode)
    .HasDatabaseName("IX_QueueItem_MissionCode");

entity.HasIndex(e => new { e.AssignedRobotId, e.Status })
    .HasDatabaseName("IX_QueueItem_RobotStatus");

entity.HasIndex(e => new { e.MapCode, e.Status })
    .HasDatabaseName("IX_QueueItem_MapStatus");

// AreaQueueConfiguration indices
entity.HasIndex(e => e.AreaCode)
    .IsUnique()
    .HasDatabaseName("IX_AreaQueue_AreaCode");
```

### Migration

```csharp
public partial class AddQueueSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QueueItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AreaCode = table.Column<string>(maxLength: 100, nullable: false),
                Priority = table.Column<int>(nullable: false),
                // ... other columns
            });

        migrationBuilder.CreateTable(
            name: "AreaQueueConfigurations",
            columns: table => new
            {
                // ... columns
            });

        // Create indices
        migrationBuilder.CreateIndex(
            name: "IX_QueueItem_AreaProcessing",
            table: "QueueItems",
            columns: new[] { "AreaCode", "Status", "Priority", "QueuePosition" });
    }
}
```

---

## 9. API Endpoints

### Queue Management

```
POST   /api/queue/enqueue           - Add task to queue
GET    /api/queue/pending/{area}    - Get pending tasks for area
GET    /api/queue/statistics        - Get queue statistics
DELETE /api/queue/{id}              - Remove task from queue
POST   /api/queue/{id}/priority     - Change task priority
```

### Area Configuration

```
GET    /api/queue/areas             - List all area configurations
POST   /api/queue/areas             - Create area configuration
PUT    /api/queue/areas/{code}      - Update area configuration
DELETE /api/queue/areas/{code}      - Delete area configuration
```

### Monitoring

```
GET    /api/queue/status            - Overall queue system status
GET    /api/queue/processing        - Currently processing tasks
GET    /api/queue/history           - Completed/failed task history
```

---

## 10. Risks and Concerns

### Critical Risks

#### 1. Race Conditions in Concurrent Processing

**Risk**: Multiple queue processors selecting the same robot simultaneously.

**Scenario**:
```
T0: Processor A checks Robot R1 → Available
T0: Processor B checks Robot R1 → Available
T1: Processor A submits Task 1 to R1
T1: Processor B submits Task 2 to R1
Result: Robot receives conflicting commands
```

**Mitigation**:
- Use database-level locking when assigning robots
- Implement optimistic concurrency with row versioning
- Consider distributed lock (Redis) for horizontal scaling

**Code Example**:
```csharp
// Use transaction with UPDLOCK hint
using var transaction = await _dbContext.Database.BeginTransactionAsync();
var queueItem = await _dbContext.QueueItems
    .FromSqlRaw("SELECT * FROM QueueItems WITH (UPDLOCK) WHERE Id = {0}", itemId)
    .FirstOrDefaultAsync();
```

#### 2. External API Latency

**Risk**: Slow robot query API causes queue processing delays.

**Scenario**:
- Robot query takes 2-5 seconds
- Queue processor blocks on each query
- Processing throughput drops significantly

**Mitigation**:
- Implement caching for robot status (short TTL: 1-2 seconds)
- Use parallel robot queries with timeout
- Add circuit breaker pattern for API failures

**Impact**: High - directly affects system responsiveness

#### 3. Strict Priority Starvation

**Risk**: Low-priority tasks never execute during continuous high-priority load.

**Scenario**:
```
9:00 AM - Priority 4 task queued
9:01 AM - Priority 1 task queued
9:02 AM - Priority 1 task queued
...
6:00 PM - Priority 4 task still waiting (9 hours)
```

**Mitigation**:
- Monitor queue wait times and alert
- Consider priority aging in future versions
- Allow manual priority override

**Acceptance**: Per design decision, this is accepted behavior. Document for operators.

#### 4. Missing Coordinates Causing Task Failures

**Risk**: Tasks rejected due to incomplete node data.

**Scenario**:
- New nodes added to system without coordinates
- Tasks referencing these nodes fail validation
- Production workflow blocked

**Mitigation**:
- Pre-import validation for node data
- Dashboard warning for nodes missing coordinates
- Sync process to update coordinates from external system

**Impact**: Medium - can block specific workflows

### High Risks

#### 5. Cross-Map Synchronization Complexity

**Risk**: Optimization logic becomes incorrect with complex map topologies.

**Concerns**:
- What if maps are not directly connected?
- What if robot must pass through intermediate maps?
- How to handle one-way paths?

**Mitigation**:
- Start with simple two-map scenarios
- Document supported topologies
- Add validation for map connectivity

#### 6. Queue State Consistency During Restart

**Risk**: System restart causes task loss or duplicate execution.

**Scenario**:
```
T0: Task dequeued, status = Processing
T1: System crashes
T2: System restarts
T3: Task stuck in Processing state forever
```

**Mitigation**:
- Implement startup recovery process
- Reset "Processing" tasks to "Pending" on startup
- Add timeout for processing state (e.g., 5 minutes)

**Recovery Code**:
```csharp
public async Task RecoverStuckTasks(CancellationToken ct)
{
    var stuckTasks = await _dbContext.QueueItems
        .Where(q => q.Status == QueueItemStatus.Processing)
        .Where(q => q.ProcessingStartedUtc < DateTime.UtcNow.AddMinutes(-5))
        .ToListAsync(ct);

    foreach (var task in stuckTasks)
    {
        task.Status = QueueItemStatus.Pending;
        task.ProcessingStartedUtc = null;
        _logger.LogWarning("Recovered stuck task {Id}", task.Id);
    }

    await _dbContext.SaveChangesAsync(ct);
}
```

#### 7. Robot Status Polling Frequency vs Accuracy

**Risk**: Stale robot status leads to incorrect decisions.

**Trade-off**:
- Fast polling (100ms): Accurate but high load
- Slow polling (5s): Low load but stale data

**Mitigation**:
- Poll on-demand during task assignment
- Cache with 1-2 second TTL
- Consider webhook from AMR system (if supported)

### Medium Risks

#### 8. Memory Pressure with Large Queues

**Risk**: Unbounded queue growth exhausts server memory.

**Scenario**:
- Peak traffic queues thousands of tasks
- Each task holds mission data JSON
- Server runs out of memory

**Mitigation**:
- Enforce MaxQueueDepth per area
- Reject new tasks when queue full (HTTP 429)
- Monitor queue sizes with alerting

#### 9. Optimization Algorithm Correctness

**Risk**: Distance-based selection may not be globally optimal.

**Example**:
```
Task A: Distance 100m, but clears path for Tasks B, C, D
Task B: Distance 50m, but blocks path for 10 minutes

Choosing Task B (shorter) is locally optimal but globally worse
```

**Mitigation**:
- Accept local optimization for initial version
- Document limitation
- Consider path-aware optimization in future

#### 10. Rollback Strategy

**Risk**: Optimization causes issues, need to quickly disable.

**Mitigation**:
- Feature flag for optimization: `EnableJobOptimization`
- Separate flag per area if needed
- Log all optimization decisions for debugging

**Configuration**:
```json
{
  "QueueSystem": {
    "EnableJobOptimization": true,
    "OptimizationLogLevel": "Information"
  }
}
```

### Low Risks

#### 11. Timezone Handling

**Risk**: Timestamps inconsistent across system components.

**Mitigation**: Use UTC throughout (already established pattern)

#### 12. Concurrent Database Migrations

**Risk**: Migration fails on active system.

**Mitigation**: Apply migrations during maintenance window

---

## 11. Open Questions

### Architecture

| # | Question | Options | Recommendation |
|---|----------|---------|----------------|
| 1 | Queue persistence during system restart? | In-memory with snapshot / Database only | Database only (simpler) |
| 2 | Maximum queue depth per area? | 100 / 500 / 1000 / Configurable | Configurable with default 500 |
| 3 | Processing interval for queue processor? | 500ms / 1s / 2s | 1 second |

### Business Logic

| # | Question | Options | Impact |
|---|----------|---------|--------|
| 4 | Timeout for stuck tasks? | 5 min / 10 min / 30 min | Affects recovery time |
| 5 | Should cancelled tasks be deleted or kept? | Delete / Soft delete / Archive | Affects storage and audit |
| 6 | Can users cancel tasks that are processing? | Yes / No | User experience vs complexity |

### Monitoring

| # | Question | Notes |
|---|----------|-------|
| 7 | What metrics should be tracked? | Queue depth, wait time, processing time, failure rate |
| 8 | What alerts should be configured? | Queue full, high wait time, high failure rate |
| 9 | How long to retain queue history? | Suggest 30 days |

### Integration

| # | Question | Notes |
|---|----------|-------|
| 10 | Should existing SavedCustomMissions be migrated to queue? | Or only new missions use queue |
| 11 | How to handle direct API submissions (bypass queue)? | Need escape hatch for urgent tasks? |
| 12 | Should frontend show queue position to users? | Transparency vs complexity |

---

## 12. Implementation Phases

### Phase 1: Basic Area Queue System

**Duration**: TBD (no time estimates per guidelines)

**Scope**:
- Database entities and migrations
- `IQueueService` implementation
- Basic `QueueProcessorBackgroundService`
- Queue management API endpoints
- Integration with SavedCustomMission trigger flow

**Deliverables**:
- Tasks queued by area code
- Strict priority processing
- Robot availability check before submission
- Queue statistics endpoint

**Not Included**:
- Job optimization
- Distance calculation
- Frontend queue UI

### Phase 2: Job Optimization

**Scope**:
- `IJobOptimizationService` implementation
- Distance calculation using QrCode coordinates
- Cross-map task prioritization
- Optimization for `OrgId = "JOBOPTIMIZE"`

**Deliverables**:
- Tasks in current map prioritized
- Distance-based selection when multiple tasks
- Coordinate validation (fail if missing)
- Optimization decision logging

### Phase 3: Frontend Integration

**Scope**:
- Queue status dashboard
- Task management UI (view, cancel, reprioritize)
- Real-time queue depth display
- Queue history viewer

**Deliverables**:
- New Angular pages for queue management
- Integration with existing mission control
- WebSocket or polling for live updates

### Phase 4: Monitoring and Optimization

**Scope**:
- Metrics collection
- Alerting integration
- Performance tuning
- Load testing

**Deliverables**:
- Grafana dashboards (or equivalent)
- Alert rules for queue health
- Performance benchmarks
- Capacity planning document

---

## Appendix A: Robot Query API Reference

### Request

```
POST /api/amr/robotQuery
```

**Parameters**:

| Name | Type | Required | Description |
|------|------|----------|-------------|
| robotId | string | No | Robot ID (query all if empty) |
| robotType | string | No | Robot type code |
| mapCode | string | No | Map code (use with floorNumber) |
| floorNumber | string | No | Floor number (use with mapCode) |

### Response

```json
{
  "data": [
    {
      "robotId": "13",
      "robotType": "KMP600I",
      "mapCode": "M001",
      "floorNumber": "A001",
      "status": 3,
      "occupyStatus": 0,
      "batteryLevel": 85.5,
      "nodeCode": "46",
      "x": "3000.0",
      "y": "15000.0",
      "robotOrientation": "90.0",
      "missionCode": null
    }
  ],
  "code": "0",
  "success": true
}
```

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| Area Code | Logical grouping of nodes/zones for queue partitioning |
| Cross-Map Workflow | Mission that moves robot from one map to another |
| Job Optimization | Algorithm to select optimal task based on robot location |
| Map Code | Unique identifier for a physical map/floor |
| Node Code | Unique identifier for a position on a map |
| OrgId | Organization identifier passed to external AMR system |
| Queue Item | Single task waiting in the queue |
| Strict Priority | Processing model where higher priority always executes first |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | Nov 2025 | AI Assistant | Initial draft |
