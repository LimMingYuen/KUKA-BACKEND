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

**Purpose:** Query job status from the AMR system.

**Endpoint:** `POST /api/missions/jobs/query`

**⚠️ Important:** Always specify `jobCode` to query a specific job. Do NOT query without filters.

**Request:**
```http
POST /api/missions/jobs/query
Content-Type: application/json
```

**Request Body (Query Specific Job - Recommended):**
```json
{
  "jobCode": "MISSION_20250116_001",
  "limit": 1
}
```

**Request Body (Query by Robot):**
```json
{
  "robotId": "Robot_A",
  "limit": 10
}
```

**Request Body (Query by Status):**
```json
{
  "status": "2",
  "limit": 50
}
```

**Request Fields (All Optional):**
- `jobCode` (string): Specific mission code
- `workflowId` (number): Workflow template ID
- `workflowCode` (string): Workflow template code
- `workflowName` (string): Workflow name
- `robotId` (string): Assigned robot ID
- `status` (string): Job status code (see status codes below)
- `containerCode` (string): Container code
- `targetCellCode` (string): Destination cell
- `createUsername` (string): User who created the mission
- `limit` (number): Maximum results (default: 10)

**Job Status Codes:**
- `0` = Created
- `2` = Executing
- `3` = Waiting
- `4` = Cancelling
- `5` = Complete
- `31` = Cancelled
- `32` = Manual Complete
- `50` = Warning
- `99` = Startup Error

**Response:**
```json
{
  "success": true,
  "code": "0",
  "message": null,
  "data": [
    {
      "jobCode": "MISSION_20250116_001",
      "workflowId": 123,
      "workflowCode": "WF001",
      "workflowName": "Rack Move",
      "robotId": "Robot_A",
      "status": 2,
      "mapCode": "Floor1",
      "targetCellCode": "C100",
      "beginCellCode": "A001",
      "completeTime": null,
      "spendTime": 45,
      "createTime": "2025-01-16 12:00:00",
      "createUsername": "admin",
      "warnFlag": 0,
      "warnCode": null
    }
  ]
}
```

**Response Fields:**
- `data` (array): Array of job objects
- `data[].jobCode` (string): Mission code
- `data[].robotId` (string): Assigned robot ID
- `data[].status` (number): Current status (see status codes above)
- `data[].completeTime` (string): Completion timestamp ("yyyy-MM-dd HH:mm:ss")
- `data[].spendTime` (number): Execution time in seconds
- `data[].warnFlag` (number): Warning indicator (0 = no warning)

**When to call:**
- ❌ **DON'T:** Poll every minute without filters (causes unnecessary load)
- ✅ **DO:** Query specific job code after submission
- ✅ **DO:** Use Queue Status API instead (see section 5)

---

## 4. Robot Query

### 4.1 Query Robot Position & Status

**Purpose:** Get real-time robot position and status.

**Endpoint:** `POST /api/missions/robot-query`

**Request (Specific Robot):**
```http
POST /api/missions/robot-query
Content-Type: application/json

{
  "robotId": "Robot_A"
}
```

**Request (All Robots on Map):**
```json
{
  "robotId": "",
  "mapCode": "Floor1"
}
```

**Request Fields:**
- `robotId` (string): Specific robot ID (empty = all robots)
- `robotType` (string): Filter by robot type
- `mapCode` (string): Filter by map/floor
- `floorNumber` (string): Filter by floor number

**Response:**
```json
{
  "success": true,
  "code": "0",
  "message": null,
  "data": [
    {
      "robotId": "Robot_A",
      "robotType": "LIFT",
      "mapCode": "Floor1",
      "floorNumber": "1",
      "status": 2,
      "occupyStatus": 1,
      "batteryLevel": 85,
      "nodeCode": "A050",
      "nodeLabel": "A050",
      "x": "125.5",
      "y": "340.2",
      "robotOrientation": "90.0",
      "missionCode": "MISSION_20250116_001",
      "liftStatus": 0,
      "reliability": 100,
      "karOsVersion": "2.1.0",
      "mileage": "1250.5"
    }
  ]
}
```

**Response Fields:**
- `robotId` (string): Robot identifier
- `status` (number): Robot status
  - `1` = Idle
  - `2` = Executing
  - `3` = Charging
  - `4` = Error
- `occupyStatus` (number): Occupancy status
  - `0` = Available
  - `1` = Occupied/Busy
- `batteryLevel` (number): Battery percentage (0-100)
- `x`, `y` (string): Robot coordinates
- `robotOrientation` (string): Robot angle in degrees (0-360)
- `missionCode` (string): Current mission (null if idle)
- `nodeLabel` (string): Current QR code/node

**When to call:**
- To display robot positions on map
- Before assigning missions manually
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

### 6.1 Recommended: Queue Status Polling

**Use Case:** Track mission progress after submission

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

  // 2. Poll for status every 3 seconds
  const pollInterval = setInterval(async () => {
    const statusResponse = await fetch(`/api/queue/status/${missionCode}`);
    const statusResult = await statusResponse.json();

    console.log(`Mission ${missionCode} status:`, statusResult.overallStatus);

    // 3. Stop polling when complete/failed/cancelled
    const terminalStatuses = ['Completed', 'Failed', 'Cancelled'];
    if (terminalStatuses.includes(statusResult.overallStatus)) {
      clearInterval(pollInterval);
      console.log(`Mission ${missionCode} finished with status:`, statusResult.overallStatus);

      // Handle completion
      if (statusResult.overallStatus === 'Completed') {
        onMissionComplete(missionCode);
      } else {
        onMissionFailed(missionCode, statusResult.queueItems[0].errorMessage);
      }
    }
  }, 3000); // Poll every 3 seconds
}
```

**Polling Interval Recommendations:**
- Mission tracking: 3-5 seconds
- Dashboard statistics: 10-15 seconds
- Robot positions (for map display): 5-10 seconds

**Stop Polling When:**
- Status is `Completed`, `Failed`, or `Cancelled`
- User navigates away from the page
- Maximum polling time reached (e.g., 10 minutes)

---

### 6.2 Not Recommended: Generic Job Query Polling

**❌ Bad Practice:**
```javascript
// DON'T DO THIS - Querying without filters
setInterval(async () => {
  const response = await fetch('/api/missions/jobs/query', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ limit: 1 })  // ❌ No jobCode filter
  });
}, 60000);
```

**Problem:** This queries ALL jobs, causing unnecessary load on the AMR system.

**✅ Better Alternative:** Use Queue Status API (section 5.1)

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

**Document Version:** 1.0
**Generated:** 2025-01-16
