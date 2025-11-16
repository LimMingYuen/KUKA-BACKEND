# Queue Scheduler Configuration

## Overview

The Queue Scheduler is a background service that automatically processes the MapCode-based mission queue system. It continuously monitors queues, assigns robots, submits jobs to the AMR system, and triggers opportunistic job evaluation.

## Configuration

Add the following section to your `appsettings.json`:

```json
{
  "QueueScheduler": {
    "Enabled": true,
    "ProcessingIntervalSeconds": 5,
    "CompletionCheckIntervalSeconds": 2,
    "MaxJobsPerMapCodePerCycle": 5,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 10,
    "EnableOpportunisticJobEvaluation": true
  }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable the queue scheduler |
| `ProcessingIntervalSeconds` | int | `5` | How often to process queues (check for pending jobs) |
| `CompletionCheckIntervalSeconds` | int | `2` | How often to check if executing jobs have completed |
| `MaxJobsPerMapCodePerCycle` | int | `5` | Maximum number of jobs to process per MapCode per processing cycle |
| `MaxRetryAttempts` | int | `3` | Maximum retry attempts for failed job submissions |
| `RetryDelaySeconds` | int | `10` | Delay between retry attempts |
| `EnableOpportunisticJobEvaluation` | bool | `true` | Enable opportunistic job chaining after job completion |

## How It Works

### 1. Queue Processing (Every 5 seconds by default)

```
For each active MapCode:
  ├─ Get pending jobs (up to MaxJobsPerMapCodePerCycle)
  ├─ For each job:
  │   ├─ Assign best robot (distance-based selection)
  │   ├─ Submit to AMR system
  │   └─ Update status (Assigned → SubmittedToAmr)
  └─ Handle failures/retries
```

### 2. Completion Monitoring (Every 2 seconds by default)

```
For each executing job:
  ├─ Query AMR system for job status
  ├─ If completed:
  │   ├─ Update status to Completed
  │   └─ Trigger opportunistic job evaluation (if enabled)
  └─ If failed:
      └─ Update status to Failed with error message
```

### 3. Opportunistic Job Evaluation

After a job completes, the system evaluates whether the robot should take another job on the same MapCode before returning home:

```
Robot completes job on MapCode B:
  ├─ Get robot's last position (from completed job)
  ├─ Find all pending jobs on MapCode B
  ├─ Calculate distance to each job's first node
  ├─ Select nearest job (priority as tie-breaker)
  ├─ Check consecutive job limit
  │   └─ If limit not reached: Assign robot to opportunistic job
  └─ If limit reached: Robot returns to original MapCode
```

## Performance Tuning

### High Throughput Environment
```json
{
  "QueueScheduler": {
    "ProcessingIntervalSeconds": 3,
    "CompletionCheckIntervalSeconds": 1,
    "MaxJobsPerMapCodePerCycle": 10
  }
}
```

### Low Resource Environment
```json
{
  "QueueScheduler": {
    "ProcessingIntervalSeconds": 10,
    "CompletionCheckIntervalSeconds": 5,
    "MaxJobsPerMapCodePerCycle": 2
  }
}
```

### Disable Opportunistic Jobs
```json
{
  "QueueScheduler": {
    "EnableOpportunisticJobEvaluation": false
  }
}
```

## Disabling the Scheduler

To disable the background queue processing:

```json
{
  "QueueScheduler": {
    "Enabled": false
  }
}
```

When disabled, jobs will remain in the queue but won't be automatically processed. You can still manually trigger processing via the API endpoints.

## Logging

The scheduler logs at various levels:

- **Information**: Job assignments, completions, opportunistic job decisions
- **Warning**: Submission failures, retry attempts
- **Error**: Unexpected errors, max retry attempts reached
- **Debug**: Queue processing cycles, no pending jobs

Example log output:
```
[INFO] Queue Scheduler starting (ProcessingInterval: 5s, CompletionCheck: 2s)
[INFO] Processing 3 pending jobs for MapCode Floor1
[INFO] Assigned robot Robot_A to job queue20250116120530 (distance: 12.45m)
[INFO] Successfully submitted job queue20250116120530 to AMR system
[INFO] Job queue20250116120530 completed successfully
[INFO] Opportunistic job evaluation for robot Robot_A: JobChained - Found nearby job (distance: 8.32m)
```

## Monitoring

### Check Scheduler Status

The scheduler runs automatically on application startup. Check logs for:
```
Queue Scheduler starting...
```

If you see:
```
Queue Scheduler is disabled
```

Then `Enabled: false` is set in configuration.

### Monitor Queue Processing

Watch for regular log entries every `ProcessingIntervalSeconds`:
```
Processing {Count} pending jobs for MapCode {MapCode}
```

No log entry means no pending jobs in any queue.

## Troubleshooting

### Jobs stuck in "Pending" status

**Possible causes:**
1. No suitable robots available on the MapCode
2. All robots are occupied
3. Robot constraints (RobotModels/RobotIDs) too restrictive
4. QR code coordinates missing for first node

**Solution:** Check logs for warnings about robot availability

### Jobs stuck in "SubmittedToAmr" status

**Possible causes:**
1. AMR system not responding to job status queries
2. Job actually executing but AMR not updating status
3. Network issues with AMR API

**Solution:** Check AMR system connectivity and job status query endpoint

### High retry counts

**Possible causes:**
1. AMR system rejecting submissions
2. Invalid mission data
3. AMR system overloaded

**Solution:** Check AMR API logs and response messages

## Related Configuration

### MapCode Queue Configuration

Each MapCode can have its own queue settings in the database (`MapCodeQueueConfigurations` table):

```sql
INSERT INTO MapCodeQueueConfigurations
(MapCode, EnableQueue, MaxConsecutiveOpportunisticJobs, EnableCrossMapOptimization, ...)
VALUES
('Floor1', 1, 1, 1, ...);
```

### System Settings

Global settings in `SystemSettings` table:

```sql
INSERT INTO SystemSetting (Key, Value) VALUES
('MinimumRobotBatteryForAssignment', '20');
```

## Dependencies

The scheduler requires these services to be properly configured:

1. **MissionService** - For submitting jobs to AMR
   ```json
   "MissionService": {
     "SubmitMissionUrl": "http://amr-system/api/missions/submit",
     "JobQueryUrl": "http://amr-system/api/jobs/query"
   }
   ```

2. **AmrServiceOptions** - For robot position queries
   ```json
   "AmrServiceOptions": {
     "RobotQueryUrl": "http://amr-system/api/robots/query"
   }
   ```

3. **Database** - Queue tables must exist (run migrations first)
   ```bash
   dotnet ef database update
   ```

## Testing

### Test Queue Processing

1. Enqueue a job:
   ```bash
   POST /api/mission-queue/enqueue
   ```

2. Watch logs for:
   ```
   Assigned robot {RobotId} to job {QueueItemCode}
   Successfully submitted job {QueueItemCode} to AMR system
   ```

3. Wait for completion:
   ```
   Job {QueueItemCode} completed successfully
   Opportunistic job evaluation for robot {RobotId}: {Decision}
   ```

### Test Opportunistic Jobs

1. Enqueue multiple jobs on same MapCode
2. Watch as first job completes
3. Check if robot chains to nearest pending job
4. Verify consecutive job limit is enforced

## See Also

- Main queue system documentation: `CLAUDE.md` (Mission Queue System section)
- API endpoints: `Controllers/MissionQueueController.cs`
- Queue manager: `Services/Queue/MapCodeQueueManager.cs`
- Opportunistic evaluation: `Services/Queue/JobOpportunityEvaluator.cs`
