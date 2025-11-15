# Workflow Node Code API Documentation

## Overview
These endpoints allow you to sync and retrieve node codes (QR code positions) for workflows from the external AMR system.

**Note:** These endpoints are **publicly accessible** (no authentication required), following the same pattern as the WorkflowsController sync endpoint.

## Recent Updates (2024-11-15)

### ✅ Removed JWT Authentication Requirement
- **Change:** Removed `[Authorize]` attribute from controller
- **Reason:** Matches the pattern of WorkflowsController which also syncs from external API
- **Benefit:** No JWT token needed - endpoints are publicly accessible

### ✅ Simplified to Sequential Processing
- **Change:** Workflows are now synced **one by one sequentially** instead of in parallel
- **Reason:** Simpler, more reliable, and eliminates all concurrency complexity
- **Benefit:** No thread-safety issues, easier to debug, and progress tracking shows "X/Total"

### ✅ Fixed: DbContext Thread-Safety
- **Solution:** Each workflow sync gets its own DbContext instance via service scopes
- **Result:** Zero entity tracking conflicts or connection issues

---

## Frontend API Endpoints

### 1. Sync All Workflows (Recommended for Initial Load)
**Endpoint:** `POST /api/workflow-node-codes/sync`

**Processing:** Workflows are synced **sequentially** (one by one) for maximum reliability.

**Example Request:**
```http
POST http://localhost:5109/api/workflow-node-codes/sync
```

**Response:**
```json
{
  "totalWorkflows": 150,
  "successCount": 148,
  "failureCount": 2,
  "nodeCodesInserted": 987,
  "nodeCodesDeleted": 23,
  "failedWorkflowIds": [45, 102],
  "errors": {
    "45": "API returned status code 404",
    "102": "Failed to obtain authentication token"
  }
}
```

**When to Use:**
- Initial data load when app starts
- Periodic refresh (e.g., once per day)
- After workflow configuration changes in external system

---

### 2. Sync Single Workflow
**Endpoint:** `POST /api/workflow-node-codes/sync/{externalWorkflowId}`

**Path Parameters:**
- `externalWorkflowId`: The external workflow ID (integer)

**Example Request:**
```http
POST http://localhost:5109/api/workflow-node-codes/sync/137
```

**Response (Success):**
```json
{
  "message": "Successfully synced workflow 137"
}
```

**Response (Failure):**
```json
{
  "error": "Failed to sync workflow 137"
}
```

**When to Use:**
- Refresh specific workflow after editing
- Retry after a failed sync

---

### 3. Get Node Codes for a Workflow
**Endpoint:** `GET /api/workflow-node-codes/{externalWorkflowId}`

**Path Parameters:**
- `externalWorkflowId`: The external workflow ID (integer)

**Example Request:**
```http
GET http://localhost:5109/api/workflow-node-codes/137
```

**Response:**
```json
[
  "Sim1-1-1",
  "Sim1-1-19",
  "Sim1-1-20",
  "Sim1-1-24",
  "Sim1-1-25",
  "Sim1-1-37",
  "Sim1-1-38"
]
```

**When to Use:**
- Display available positions for a workflow
- Populate dropdowns/selectors in mission creation
- Validate node selections

---

## Typical Frontend Flow

### Initial App Load (One-Time Setup)
```javascript
// Sync all workflows on app initialization or admin action
async function syncAllWorkflowNodeCodes() {
  try {
    const response = await fetch('/api/workflow-node-codes/sync', {
      method: 'POST'
    });

    const result = await response.json();
    console.log(`Synced ${result.successCount}/${result.totalWorkflows} workflows`);
    console.log(`Inserted: ${result.nodeCodesInserted}, Deleted: ${result.nodeCodesDeleted}`);

    if (result.failureCount > 0) {
      console.warn(`${result.failureCount} workflows failed:`, result.errors);
    }
  } catch (error) {
    console.error('Sync failed:', error);
  }
}
```

### Workflow Selection (Real-Time)
```javascript
// Get node codes when user selects a workflow
async function getNodeCodesForWorkflow(externalWorkflowId) {
  try {
    const response = await fetch(`/api/workflow-node-codes/${externalWorkflowId}`);
    const nodeCodes = await response.json();

    // Populate UI dropdown/selector
    populateNodeCodeDropdown(nodeCodes);
  } catch (error) {
    console.error('Failed to get node codes:', error);
  }
}
```

### Refresh Single Workflow (After Edit)
```javascript
// Refresh specific workflow after configuration changes
async function refreshWorkflow(externalWorkflowId) {
  try {
    const response = await fetch(`/api/workflow-node-codes/sync/${externalWorkflowId}`, {
      method: 'POST'
    });

    // Reload node codes
    await getNodeCodesForWorkflow(externalWorkflowId);
  } catch (error) {
    console.error('Refresh failed:', error);
  }
}
```

---

## Important Notes

1. **No Authentication Required**: All endpoints are publicly accessible (following the same pattern as WorkflowsController)

2. **ExternalWorkflowId**: This is NOT the local database workflow ID, it's the ID from the external AMR system (stored in `WorkflowDiagram.ExternalWorkflowId`)

3. **Sequential Processing**: Workflows are synced one by one for maximum reliability. The sync shows progress in logs: "Syncing workflow 1/301", "Syncing workflow 2/301", etc.

4. **Data Freshness**: Node codes are cached in the database. Call sync endpoints to refresh data from the external system

5. **Error Handling**: Always check the response for errors and handle failed syncs appropriately

6. **Sync Duration**: For 301 workflows, expect the full sync to take a few minutes since it processes sequentially

---

## Recommended UI Features

### Admin Panel
- **Bulk Sync Button**: Triggers `POST /api/workflow-node-codes/sync`
- **Progress Indicator**: Shows sync progress and results
- **Error Display**: Shows failed workflows and error messages

### Mission Creation Form
- **Workflow Dropdown**: Populated from `/api/workflows` endpoint
- **Node Code Selector**: Populated from `GET /api/workflow-node-codes/{id}` when workflow is selected
- **Refresh Button**: Triggers `POST /api/workflow-node-codes/sync/{id}` for selected workflow

---

## Example React Component

```jsx
import { useState, useEffect } from 'react';

function WorkflowNodeCodeSelector({ workflowId, onNodeCodeSelect }) {
  const [nodeCodes, setNodeCodes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (workflowId) {
      fetchNodeCodes();
    }
  }, [workflowId]);

  const fetchNodeCodes = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/workflow-node-codes/${workflowId}`,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (!response.ok) throw new Error('Failed to fetch node codes');

      const data = await response.json();
      setNodeCodes(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = async () => {
    setLoading(true);
    try {
      // Sync from external API
      await fetch(`/api/workflow-node-codes/sync/${workflowId}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });

      // Reload node codes
      await fetchNodeCodes();
    } catch (err) {
      setError(err.message);
    }
  };

  if (loading) return <div>Loading node codes...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <select onChange={(e) => onNodeCodeSelect(e.target.value)}>
        <option value="">Select Node Code</option>
        {nodeCodes.map(code => (
          <option key={code} value={code}>{code}</option>
        ))}
      </select>
      <button onClick={handleRefresh}>Refresh</button>
    </div>
  );
}
```
