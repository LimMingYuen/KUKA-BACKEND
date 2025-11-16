# Frontend Integration Guide
## QES KUKA AMR API - Complete Reference

**Version:** 1.0
**Last Updated:** 2025-01-16
**Base URL:** `http://localhost:5109/api`

---

## Table of Contents

1. [Initial Setup & Sync Operations](#1-initial-setup--sync-operations)
2. [Mission Submission](#2-mission-submission)
3. [Job Query (Status Tracking)](#3-job-query-status-tracking)
4. [Robot Query](#4-robot-query)
5. [Queue Status Tracking (Recommended)](#5-queue-status-tracking-recommended)
6. [Polling Patterns](#6-polling-patterns)
7. [Common Workflows](#7-common-workflows)
8. [Error Handling](#8-error-handling)

---

## 1. Initial Setup & Sync Operations

### 1.1 Sync Workflows

**Purpose:** Pull workflow templates from the AMR system into the local database.

**Endpoint:** `POST /api/workflows/sync`

**Authentication:** Requires JWT Bearer token (automatically obtained from external API)

**Request:**
```http
POST /api/workflows/sync
Content-Type: application/json
```

No request body needed.

**Response:**
```json
{
  "total": 15,
  "inserted": 10,
  "updated": 5
}
```

**Response Fields:**
- `total` (number): Total workflows processed
- `inserted` (number): New workflows added to database
- `updated` (number): Existing workflows updated

**When to call:**
- On application startup (once)
- When workflow templates are added/modified in the AMR system
- Recommended: Add a "Sync Workflows" button in admin panel

---

### 1.2 Get Workflows List

**Purpose:** Retrieve all synced workflow templates.

**Endpoint:** `GET /api/workflows`

**Request:**
```http
GET /api/workflows
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Rack Move Workflow",
    "number": "WF001",
    "externalCode": "WF001",
    "status": 1,
    "layoutCode": "Floor1",
    "activeSchedulesCount": 0
  },
  {
    "id": 2,
    "name": "Delivery Workflow",
    "number": "WF002",
    "externalCode": "WF002",
    "status": 1,
    "layoutCode": "Floor2",
    "activeSchedulesCount": 0
  }
]
```

**Response Fields:**
- `id` (number): Internal database ID
- `name` (string): Workflow display name
- `number` (string): Workflow code/number
- `externalCode` (string): Code used in external AMR system
- `status` (number): Workflow status (1 = active)
- `layoutCode` (string): Map/floor code where workflow operates
- `activeSchedulesCount` (number): Number of active schedules (deprecated, always 0)

**When to call:**
- To populate workflow selection dropdowns
- After syncing workflows
- On page load for workflow management screens

---

### 1.3 Sync QR Codes (Node Positions)

**Purpose:** Sync QR code/node positions from the AMR system.

**Endpoint:** `POST /api/qrcodes/sync`

**Authentication:** Requires JWT Bearer token (automatically obtained)

**Request:**
```http
POST /api/qrcodes/sync
Content-Type: application/json
```

No request body needed.

**Response:**
```json
{
  "total": 250,
  "inserted": 200,
  "updated": 50
}
```

**When to call:**
- On application startup
- When QR codes/nodes are added/modified
- Recommended: Add a "Sync QR Codes" button in admin panel

---

### 1.4 Get QR Codes List

**Endpoint:** `GET /api/qrcodes`

**Response:**
```json
[
  {
    "id": 1,
    "nodeLabel": "A001",
    "mapCode": "Floor1",
    "floorNumber": "1",
    "nodeNumber": 1,
    "reliability": 100,
    "reportTimes": 1500,
    "lastUpdateTime": "2025-01-16 10:30:00"
  }
]
```

---

## 2. Mission Submission

### 2.1 Submit Mission

**Purpose:** Submit a new mission to the queue for robot execution.

**Endpoint:** `POST /api/missions/submit`

**Request:**
```http
POST /api/missions/submit
Content-Type: application/json
```

**Request Body:**
```json
{
  "orgId": "",
  "requestId": "REQ_20250116_001",
  "missionCode": "MISSION_20250116_001",
  "missionType": "RACK_MOVE",
  "viewBoardType": "",
  "robotModels": [],
  "robotIds": [],
  "robotType": "LIFT",
  "priority": 50,
  "containerModelCode": "",
  "containerCode": "",
  "templateCode": "WF001",
  "lockRobotAfterFinish": false,
  "unlockRobotId": "",
  "unlockMissionCode": "",
  "idleNode": "",
  "missionData": [
    {
      "sequence": 1,
      "position": "A001",
      "type": "MOVE",
      "putDown": false,
      "passStrategy": "NORMAL",
      "waitingMillis": 0
    },
    {
      "sequence": 2,
      "position": "B050",
      "type": "PICK",
      "putDown": false,
      "passStrategy": "NORMAL",
      "waitingMillis": 2000
    },
    {
      "sequence": 3,
      "position": "C100",
      "type": "DROP",
      "putDown": true,
      "passStrategy": "NORMAL",
      "waitingMillis": 2000
    }
  ]
}
```

**Request Fields:**

**Required:**
- `missionCode` (string): Unique mission identifier (e.g., "MISSION_20250116_001")
- `priority` (number): Mission priority (0-100, higher = more urgent)
- `missionData` (array): Mission steps (see MissionDataItem structure below)

**Optional:**
- `requestId` (string): Client request tracking ID
- `missionType` (string): Type of mission (e.g., "RACK_MOVE", "DELIVERY")
- `robotType` (string): Required robot type (e.g., "LIFT", "LATENT")
- `robotModels` (array): Allowed robot models (empty = any model)
- `robotIds` (array): Specific robot IDs (empty = any robot)
- `templateCode` (string): Workflow template code
- `priority` (number): 0-100 (default: 50)
- `lockRobotAfterFinish` (boolean): Lock robot after completion

**MissionDataItem Structure:**
- `sequence` (number): Step order (1, 2, 3...)
- `position` (string): QR code/node label (e.g., "A001")
- `type` (string): Action type ("MOVE", "PICK", "DROP", "WAIT")
- `putDown` (boolean): Whether to put down cargo
- `passStrategy` (string): Navigation strategy ("NORMAL", "SLOW", "FAST")
- `waitingMillis` (number): Wait time in milliseconds

**Response (Success):**
```json
{
  "success": true,
  "code": "QUEUED",
  "message": "Mission queued successfully",
  "requestId": "REQ_20250116_001",
  "data": {
    "queueItemCodes": ["queue20250116120530"],
    "queueItemCount": 1,
    "isMultiMap": false,
    "primaryMapCode": "Floor1"
  }
}
```

**Response Fields:**
- `success` (boolean): Operation status
- `code` (string): Status code ("QUEUED" = successfully enqueued)
- `message` (string): Human-readable message
- `requestId` (string): Echo of request ID
- `data.queueItemCodes` (array): Queue item tracking codes
- `data.queueItemCount` (number): Number of queue segments created
- `data.isMultiMap` (boolean): Whether mission spans multiple maps
- `data.primaryMapCode` (string): Primary map/floor code

**Response (Error):**
```json
{
  "success": false,
  "code": "INVALID_REQUEST",
  "message": "Mission data is required"
}
```

---

## 3. Job Query (Status Tracking)

### 3.1 Query Job Status

**Purpose:** Query job status from the AMR system to get robot assignment and execution status.

**Endpoint:** `POST /api/missions/jobs/query`

**⚠️ CRITICAL:** Always specify `jobCode` parameter. **NEVER** send request with all null fields!

**Request:**
```http
POST /api/missions/jobs/query
Content-Type: application/json
```

**Request Body (Correct - Query Specific Job):**
```json
{
  "jobCode": "MISSION_20250116_001",
  "limit": 10
}
```

**❌ WRONG - Do NOT send this:**
```json
{
  "workflowId": null,
  "containerCode": null,
  "jobCode": null,
  "status": null,
  "robotId": null,
  "limit": 1
}
```

**Request Fields (All Optional):**
- `jobCode` (string): **REQUIRED for tracking** - Specific mission code
- `workflowId` (number): Workflow template ID
- `workflowCode` (string): Workflow template code
- `workflowName` (string): Workflow name
- `robotId` (string): Assigned robot ID
- `status` (string): Job status code (see status codes below)
- `containerCode` (string): Container code
- `targetCellCode` (string): Destination cell
- `createUsername` (string): User who created the mission
- `sourceValue` (number): Source type (2=Interface, 3=PDA, 4=Device, 5=MLS, 6=Fleet, 7=Workflow event)
- `maps` (array of strings): Filter by map codes
- `limit` (number): Maximum results (default: 10)

**Job Status Codes (from AMR System):**
- `10` = Created
- `20` = Executing
- `25` = Waiting
- `28` = Cancelling
- `30` = Complete
- `31` = Cancelled
- `35` = Manual Complete
- `50` = Warning
- `60` = Startup Error

**Response:**
```json
{
  "success": true,
  "code": "0",
  "message": null,
  "data": [
    {
      "jobCode": "MISSION_20250116_001",
      "workflowId": 100218,
      "workflowCode": "WF001",
      "workflowName": "Rack Move",
      "robotId": "Robot_A",
      "status": 20,
      "workflowPriority": 50,
      "mapCode": "Floor1",
      "targetCellCode": "C100",
      "beginCellCode": "A001",
      "targetCellCodeForeign": "DROPPOINT",
      "beginCellCodeForeign": "PICKPOINT",
      "finalNodeCode": "C100",
      "completeTime": null,
      "spendTime": 45,
      "createTime": "2025-01-16 12:00:00",
      "createUsername": "admin",
      "source": "INTERFACE",
      "warnFlag": 0,
      "warnCode": null,
      "materialsInfo": "-"
    }
  ]
}
```

**Response Fields:**
- `data` (array): Array of job objects
- `data[].jobCode` (string): Mission code (same as mission code)
- `data[].robotId` (string): **Assigned robot ID** - Use this for Robot Query
- `data[].status` (number): Current status (see status codes above)
- `data[].createTime` (string): Job creation time ("yyyy-MM-dd HH:mm:ss")
- `data[].completeTime` (string): Completion time ("yyyy-MM-dd HH:mm:ss")
- `data[].spendTime` (number): Execution time in seconds
- `data[].mapCode` (string): Map code where job is executing
- `data[].targetCellCode` (string): Target node code
- `data[].beginCellCode` (string): Start node code
- `data[].warnFlag` (number): Warning indicator (0 = Normal, 1 = Warning)

**Use Case:**
1. After submitting a mission, poll this endpoint to check status
2. Extract `robotId` from response
3. Use `robotId` to call Robot Query API (section 4) to get robot's current node

---

## 4. Robot Query

### 4.1 Query Robot Position & Status

**Purpose:** Get real-time robot position, current node, and status. **Use the `robotId` from Job Query response.**

**Endpoint:** `POST /api/missions/robot-query`

**⚠️ Important:** Use `robotId` and `robotType` from Job Query response (section 3.1).

**Request (Specific Robot - Recommended):**
```http
POST /api/missions/robot-query
Content-Type: application/json

{
  "robotId": "Robot_A",
  "robotType": "KMP600I"
}
```

**Request (All Robots on Map):**
```json
{
  "robotId": "",
  "mapCode": "Floor1",
  "floorNumber": "1"
}
```

**Request Fields (All Optional):**
- `robotId` (string): Specific robot ID (empty = query all robots)
- `robotType` (string): Robot type code (e.g., "KMP600I", "LIFT")
- `mapCode` (string): Filter by map code
- `floorNumber` (string): Filter by floor number

**Note:** If querying all robots, `mapCode` and `floorNumber` must be passed together.

**Response:**
```json
{
  "success": true,
  "code": "0",
  "message": null,
  "data": [
    {
      "robotId": "Robot_A",
      "robotType": "KMP600I",
      "mapCode": "Floor1",
      "floorNumber": "1",
      "buildingCode": "W001",
      "containerCode": "",
      "status": 4,
      "occupyStatus": 1,
      "batteryLevel": 85,
      "nodeCode": "A050",
      "nodeLabel": "A050",
      "nodeNumber": 50,
      "x": "3000.0",
      "y": "15000.0",
      "robotOrientation": "90.0",
      "missionCode": "MISSION_20250116_001",
      "liftStatus": 0,
      "reliability": 1,
      "runTime": "6745",
      "karOsVersion": "2.1.0",
      "mileage": "1250.5",
      "leftMotorTemperature": "0.0",
      "rightMotorTemperature": "0.0",
      "rotateMotorTemperature": "0.0",
      "liftMtrTemp": "0.0",
      "leftFrtMovMtrTemp": "0.0",
      "rightFrtMovMtrTemp": "0.0",
      "leftReMovMtrTemp": "0.0",
      "rightReMovMtrTemp": "0.0",
      "rotateTimes": 0,
      "liftTimes": 0,
      "nodeForeignCode": "",
      "errorMessage": null
    }
  ]
}
```

**Response Fields:**
- `robotId` (string): Robot identifier
- `robotType` (string): Robot type code
- `mapCode` (string): Current map code
- `floorNumber` (string): Current floor number
- `status` (number): Robot status
  - `1` = Departure
  - `2` = Offline
  - `3` = Idle
  - `4` = Executing
  - `5` = Charging
  - `6` = Updating
  - `7` = Abnormal
- `occupyStatus` (number): Occupancy status
  - `0` = Idle
  - `1` = Occupied
- `batteryLevel` (number): Battery percentage (0-100)
- `nodeCode` (string): **Current node code**
- `nodeLabel` (string): **Current node label**
- `x`, `y` (string): Robot coordinates in millimeters
- `robotOrientation` (string): Robot angle in degrees (0-360)
- `missionCode` (string): Current mission code (null if idle)
- `liftStatus` (number): Lift status (1 = Lifted, 0 = Down)
- `reliability` (number): Location reliability (0 = Unreliable, 1 = Reliable)
- `runTime` (string): Total run time
- `mileage` (string): Total mileage

**Use Case:**
1. After Job Query returns `robotId` (section 3.1)
2. Call Robot Query with that `robotId`
3. Get robot's current `nodeCode` to track position
4. Display robot on map using `x`, `y`, `robotOrientation`

**When to call:**
- After getting `robotId` from Job Query
- To display robot positions on map
- For robot status dashboard
- **Note:** Backend auto-caches robot positions every 30 seconds

---

## 5. Queue Status Tracking (Recommended)

### 5.1 Get Mission Status by Mission Code

**Purpose:** Track mission progress through the queue system.

**Endpoint:** `GET /api/queue/status/{missionCode}`

**✅ Recommended:** Use this instead of Job Query for tracking submitted missions.

**Request:**
```http
GET /api/queue/status/MISSION_20250116_001
```

**Response:**
```json
{
  "missionCode": "MISSION_20250116_001",
  "totalSegments": 1,
  "overallStatus": "Executing",
  "queueItems": [
    {
      "queueItemId": 123,
      "queueItemCode": "queue20250116120530",
      "missionCode": "MISSION_20250116_001",
      "status": "Executing",
      "priority": 50,
      "primaryMapCode": "Floor1",
      "assignedRobotId": "Robot_A",
      "enqueuedUtc": "2025-01-16T12:05:30Z",
      "startedUtc": "2025-01-16T12:05:35Z",
      "completedUtc": null,
      "cancelledUtc": null,
      "errorMessage": null,
      "retryCount": 0,
      "isOpportunisticJob": false,
      "hasNextSegment": false
    }
  ]
}
```

**Queue Item Status Values:**
- `Pending` - Waiting in queue
- `ReadyToAssign` - Ready for robot assignment
- `Assigned` - Robot assigned, preparing to submit
- `SubmittedToAmr` - Sent to AMR system
- `Executing` - Robot is executing
- `Completed` - Successfully finished
- `Failed` - Failed after retries
- `Cancelled` - Manually cancelled

**Overall Status Values:**
- `Pending` - All items pending
- `Assigned` - At least one assigned
- `SubmittedToAmr` - At least one submitted
- `Executing` - At least one executing
- `Completed` - All items completed
- `Failed` - At least one failed
- `Cancelled` - At least one cancelled

---

### 5.2 Get Queue Item Details

**Endpoint:** `GET /api/queue/{queueItemId}`

**Request:**
```http
GET /api/queue/123
```

**Response:** Same as `queueItems[0]` object from section 5.1

---

### 5.3 Get MapCode Queue Status

**Purpose:** Monitor all jobs on a specific map/floor.

**Endpoint:** `GET /api/queue/mapcode/{mapCode}`

**Request:**
```http
GET /api/queue/mapcode/Floor1?status=Executing&limit=100
```

**Query Parameters:**
- `status` (string): Filter by status (optional)
- `limit` (number): Max results (default: 100)

**Response:**
```json
{
  "mapCode": "Floor1",
  "pendingCount": 5,
  "processingCount": 3,
  "completedCount": 42,
  "queueItems": [...]
}
```

---

### 5.4 Get Queue Statistics

**Purpose:** Dashboard overview of all queues.

**Endpoint:** `GET /api/queue/statistics`

**Response:**
```json
{
  "totalPending": 12,
  "totalProcessing": 5,
  "totalCompleted": 150,
  "totalFailed": 2,
  "generatedAt": "2025-01-16T12:10:00Z",
  "mapCodeStatistics": [
    {
      "mapCode": "Floor1",
      "pendingCount": 8,
      "readyToAssignCount": 2,
      "assignedCount": 1,
      "executingCount": 2,
      "completedCount": 100,
      "failedCount": 1,
      "cancelledCount": 0,
      "totalCount": 114
    },
    {
      "mapCode": "Floor2",
      "pendingCount": 4,
      "executingCount": 3,
      "completedCount": 50,
      "totalCount": 57
    }
  ]
}
```

---

### 5.5 Get Robot Current Job

**Purpose:** Check what job a specific robot is working on.

**Endpoint:** `GET /api/queue/robot/{robotId}/current`

**Request:**
```http
GET /api/queue/robot/Robot_A/current
```

**Response:**
```json
{
  "robotId": "Robot_A",
  "hasActiveJob": true,
  "currentJob": {
    "queueItemId": 123,
    "missionCode": "MISSION_20250116_001",
    "status": "Executing",
    "priority": 50,
    ...
  }
}
```

---

### 5.6 Cancel Queue Item

**Endpoint:** `POST /api/queue/{queueItemId}/cancel`

**Request:**
```http
POST /api/queue/123/cancel
```

**Response:**
```json
{
  "success": true,
  "message": "Mission cancelled successfully",
  "queueItemId": 123,
  "cancelledUtc": "2025-01-16T12:15:00Z"
}
```

---

## 6. Polling Patterns

### 6.1 Correct Pattern: Job Query + Robot Query Polling

**Use Case:** Track mission execution and robot position after submission

**Pattern:**
```javascript
// After submitting a mission
async function submitAndTrackMission(missionRequest) {
  // 1. Submit mission
  const submitResponse = await fetch('/api/missions/submit', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(missionRequest)
  });

  const submitResult = await submitResponse.json();

  if (!submitResult.success) {
    console.error('Mission submission failed:', submitResult.message);
    return;
  }

  const missionCode = missionRequest.missionCode;
  console.log('Mission queued:', submitResult.data.queueItemCodes);

  let currentRobotId = null;
  let currentRobotType = null;

  // 2. Poll Job Query every 3 seconds
  const jobPollInterval = setInterval(async () => {
    // ✅ CORRECT: Always specify jobCode
    const jobResponse = await fetch('/api/missions/jobs/query', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        jobCode: missionCode,
        limit: 10
      })
    });

    const jobResult = await jobResponse.json();

    if (jobResult.success && jobResult.data && jobResult.data.length > 0) {
      const job = jobResult.data[0];

      console.log(`Job ${missionCode} status:`, job.status);
      console.log(`Create time:`, job.createTime);

      // Update UI with job status
      updateJobStatus(job);

      // Extract robotId and robotType
      if (job.robotId && job.status === 20) { // 20 = Executing
        currentRobotId = job.robotId;
        currentRobotType = job.robotType || 'LIFT';

        // Start robot position polling
        if (!robotPollInterval) {
          startRobotPolling(currentRobotId, currentRobotType);
        }
      }

      // 3. Stop polling when complete
      if (job.status === 30 || job.status === 31) { // 30 = Complete, 31 = Cancelled
        clearInterval(jobPollInterval);
        if (robotPollInterval) {
          clearInterval(robotPollInterval);
        }

        console.log(`Mission ${missionCode} finished with status:`, job.status);
        onMissionComplete(job);
      }
    }
  }, 3000); // Poll every 3 seconds

  // Robot position polling
  let robotPollInterval = null;

  function startRobotPolling(robotId, robotType) {
    robotPollInterval = setInterval(async () => {
      // Query robot position
      const robotResponse = await fetch('/api/missions/robot-query', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          robotId: robotId,
          robotType: robotType
        })
      });

      const robotResult = await robotResponse.json();

      if (robotResult.success && robotResult.data && robotResult.data.length > 0) {
        const robot = robotResult.data[0];

        console.log(`Robot ${robotId} at node:`, robot.nodeCode);
        console.log(`Robot position: (${robot.x}, ${robot.y})`);
        console.log(`Battery: ${robot.batteryLevel}%`);

        // Update robot position on map
        updateRobotOnMap(robot);
      }
    }, 5000); // Poll every 5 seconds
  }

  // Auto-stop after 10 minutes
  setTimeout(() => {
    clearInterval(jobPollInterval);
    if (robotPollInterval) {
      clearInterval(robotPollInterval);
    }
  }, 600000);
}
```

**Polling Interval Recommendations:**
- Job Query (status tracking): 3-5 seconds
- Robot Query (position tracking): 5 seconds
- Dashboard statistics: 10-15 seconds

**Stop Polling When:**
- Job status is `30` (Complete), `31` (Cancelled), or `35` (Manual Complete)
- User navigates away from the page
- Maximum polling time reached (e.g., 10 minutes)

---

### 6.2 ❌ WRONG: Generic Job Query Without jobCode

**Bad Practice:**
```javascript
// ❌ DON'T DO THIS - Querying without jobCode
setInterval(async () => {
  const response = await fetch('/api/missions/jobs/query', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      workflowId: null,
      containerCode: null,
      jobCode: null,      // ❌ Missing jobCode!
      status: null,
      robotId: null,
      limit: 1
    })
  });
}, 60000);
```

**Problem:** This queries ALL jobs with no filters, causing:
- Unnecessary load on the AMR system
- Returns empty data if no jobs exist
- Wastes bandwidth and resources

**✅ Correct:** Always specify `jobCode`:
```javascript
// ✅ DO THIS
const response = await fetch('/api/missions/jobs/query', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    jobCode: "MISSION_20250116_001",  // ✅ Always specify jobCode
    limit: 10
  })
});
```

---

### 6.3 Alternative: Queue Status API Polling (Simpler)

If you prefer a simpler approach, use the Queue Status API instead of Job Query:

```javascript
// Simpler alternative using Queue Status API
const pollInterval = setInterval(async () => {
  const response = await fetch(`/api/queue/status/${missionCode}`);
  const status = await response.json();

  console.log('Overall status:', status.overallStatus);
  console.log('Robot:', status.queueItems[0].assignedRobotId);

  if (['Completed', 'Failed', 'Cancelled'].includes(status.overallStatus)) {
    clearInterval(pollInterval);
  }
}, 3000);
```

---

## 7. Common Workflows

### 7.1 Complete Mission Submission Flow

```javascript
// 1. Initial Setup (once on app start)
async function initializeApp() {
  // Sync workflows
  await fetch('/api/workflows/sync', { method: 'POST' });

  // Sync QR codes
  await fetch('/api/qrcodes/sync', { method: 'POST' });

  // Get workflow list for UI
  const workflowsResponse = await fetch('/api/workflows');
  const workflows = await workflowsResponse.json();

  populateWorkflowDropdown(workflows);
}

// 2. Submit Mission
async function submitMission() {
  const missionRequest = {
    missionCode: generateMissionCode(),
    priority: 50,
    robotType: "LIFT",
    missionData: [
      { sequence: 1, position: "A001", type: "MOVE", putDown: false, passStrategy: "NORMAL", waitingMillis: 0 },
      { sequence: 2, position: "B050", type: "PICK", putDown: false, passStrategy: "NORMAL", waitingMillis: 2000 },
      { sequence: 3, position: "C100", type: "DROP", putDown: true, passStrategy: "NORMAL", waitingMillis: 2000 }
    ]
  };

  const response = await fetch('/api/missions/submit', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(missionRequest)
  });

  const result = await response.json();

  if (result.success) {
    showNotification('Mission queued successfully');
    startPollingStatus(missionRequest.missionCode);
  } else {
    showError(result.message);
  }
}

// 3. Track Mission Status
function startPollingStatus(missionCode) {
  const pollInterval = setInterval(async () => {
    const response = await fetch(`/api/queue/status/${missionCode}`);
    const status = await response.json();

    updateMissionUI(status);

    if (['Completed', 'Failed', 'Cancelled'].includes(status.overallStatus)) {
      clearInterval(pollInterval);
      handleMissionComplete(status);
    }
  }, 3000);

  // Auto-stop after 10 minutes
  setTimeout(() => clearInterval(pollInterval), 600000);
}
```

---

### 7.2 Dashboard with Live Updates

```javascript
// Update dashboard every 10 seconds
async function updateDashboard() {
  // Get queue statistics
  const statsResponse = await fetch('/api/queue/statistics');
  const stats = await statsResponse.json();

  displayQueueStatistics(stats);

  // Get robot positions for map
  const robotsResponse = await fetch('/api/missions/robot-query', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ robotId: '', mapCode: 'Floor1' })
  });

  const robotsData = await robotsResponse.json();
  updateRobotMap(robotsData.data);
}

// Run every 10 seconds
setInterval(updateDashboard, 10000);
```

---

### 7.3 Mission Cancellation

```javascript
async function cancelMission(queueItemId) {
  const confirmCancel = confirm('Are you sure you want to cancel this mission?');

  if (!confirmCancel) return;

  const response = await fetch(`/api/queue/${queueItemId}/cancel`, {
    method: 'POST'
  });

  const result = await response.json();

  if (result.success) {
    showNotification('Mission cancelled successfully');
    refreshMissionList();
  } else {
    showError(result.message);
  }
}
```

---

## 8. Error Handling

### 8.1 Common HTTP Status Codes

- `200 OK` - Success
- `400 Bad Request` - Invalid request parameters
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error
- `502 Bad Gateway` - External AMR system unreachable

### 8.2 Error Response Format

```json
{
  "success": false,
  "code": "ERROR_CODE",
  "message": "Human-readable error message"
}
```

### 8.3 Error Handling Best Practices

```javascript
async function safeFetch(url, options) {
  try {
    const response = await fetch(url, options);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    if (data.success === false) {
      throw new Error(data.message || 'Operation failed');
    }

    return data;
  } catch (error) {
    console.error('API Error:', error);
    showErrorNotification(error.message);
    throw error;
  }
}

// Usage
try {
  const result = await safeFetch('/api/missions/submit', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(missionRequest)
  });

  console.log('Mission submitted:', result.data);
} catch (error) {
  // Error already logged and displayed
}
```

---

## 9. Automatic Background Processing

### 9.1 Queue Scheduler (Backend)

**Important:** The backend automatically processes jobs in the background. You don't need to trigger this manually.

**What happens automatically:**
1. **Every 5 seconds:** Backend checks for pending jobs and assigns robots
2. **Every 2 seconds:** Backend checks executing jobs for completion
3. **Automatic robot queries:** Backend queries robot positions when needed (cached for 30 seconds)
4. **Automatic job queries:** Backend queries AMR system for job status

**You only need to:**
- Submit missions via `/api/missions/submit`
- Poll `/api/queue/status/{missionCode}` to track progress
- Display status updates to users

---

## 10. Quick Reference

### Essential Endpoints Summary

| Purpose | Method | Endpoint | Poll? |
|---------|--------|----------|-------|
| Sync Workflows | POST | `/api/workflows/sync` | No |
| Get Workflows | GET | `/api/workflows` | No |
| Submit Mission | POST | `/api/missions/submit` | No |
| **Track Mission** | GET | `/api/queue/status/{missionCode}` | ✅ Yes (3-5s) |
| Queue Statistics | GET | `/api/queue/statistics` | Yes (10-15s) |
| Robot Positions | POST | `/api/missions/robot-query` | Yes (5-10s) |
| Cancel Mission | POST | `/api/queue/{queueItemId}/cancel` | No |

---

## Need Help?

- **API Base URL:** `http://localhost:5109/api`
- **Swagger Documentation:** `http://localhost:5109/swagger`
- **Backend Logs:** Check `QES-KUKA-AMR-API/Logs/` directory

---

---

## 11. Troubleshooting Common Issues

### 11.1 Problem: Job Query Returns Empty Data

**Symptoms:**
```
QueryJobsAsync Response - Status: OK, Body: {"data":[],"code":null,"message":null,"success":true}
```

**Root Cause:**
Frontend is calling `/api/missions/jobs/query` with **ALL NULL FIELDS**:
```json
{
  "workflowId": null,
  "containerCode": null,
  "jobCode": null,      // ❌ Problem: No filter!
  "status": null,
  "robotId": null,
  "limit": 1
}
```

**Solution:**
Always specify `jobCode` parameter:
```json
{
  "jobCode": "MISSION_20250116_001",  // ✅ Correct
  "limit": 10
}
```

**What to check in frontend code:**
1. Search for `fetch('/api/missions/jobs/query'`
2. Find the request body construction
3. Ensure `jobCode` is always set to the mission code you're tracking
4. Remove any code that sends null/undefined values for all fields

**Example Fix:**
```javascript
// ❌ BEFORE (Wrong)
const jobQueryRequest = {
  workflowId: null,
  jobCode: null,
  status: null,
  limit: 1
};

// ✅ AFTER (Correct)
const jobQueryRequest = {
  jobCode: missionCode,  // Use the mission code from submit response
  limit: 10
};
```

---

### 11.2 Problem: Don't Know Robot's Current Node

**Solution:** Use the **Job Query → Robot Query** pattern:

```javascript
// 1. Query job status to get robotId
const jobResponse = await fetch('/api/missions/jobs/query', {
  method: 'POST',
  body: JSON.stringify({ jobCode: missionCode, limit: 10 })
});

const jobData = await jobResponse.json();
const robotId = jobData.data[0].robotId;
const robotType = jobData.data[0].robotType;

// 2. Query robot to get current node
const robotResponse = await fetch('/api/missions/robot-query', {
  method: 'POST',
  body: JSON.stringify({ robotId: robotId, robotType: robotType })
});

const robotData = await robotResponse.json();
const currentNode = robotData.data[0].nodeCode;

console.log(`Robot ${robotId} is at node ${currentNode}`);
```

---

### 11.3 Quick Fix Checklist

If your frontend is experiencing the empty job query issue:

- [ ] Find where `/api/missions/jobs/query` is called
- [ ] Verify `jobCode` parameter is set to the mission code
- [ ] Remove any code setting all fields to `null`
- [ ] Test with a specific mission code
- [ ] Verify response contains job data
- [ ] Extract `robotId` from job response
- [ ] Use `robotId` in robot query to get current node

---

**Document Version:** 1.1
**Last Updated:** 2025-01-16
**Changelog:**
- v1.1: Added correct job query and robot query patterns based on AMR system API documentation
- v1.1: Added troubleshooting section for common frontend issues
- v1.0: Initial version
