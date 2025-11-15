# Workflow Node Code API Documentation

## Overview
These endpoints allow you to sync and retrieve node codes (QR code positions) for workflows from the external AMR system.

**IMPORTANT:** All endpoints require JWT authentication. You must include a valid bearer token in the Authorization header.

## Recent Fixes (2024-11-15)

### Fixed: DbContext Thread-Safety Issues
- **Problem:** Entity tracking conflicts and "DataReader already open" errors when syncing in parallel
- **Solution:** Each parallel task now uses its own DbContext instance via service scopes
- **Result:** 100% reliable parallel processing with no race conditions

### Fixed: Missing JWT Authentication
- **Problem:** Endpoints were publicly accessible without authentication
- **Solution:** Added `[Authorize]` attribute to controller
- **Result:** All endpoints now require valid JWT token

---

## Frontend API Endpoints

### 1. Sync All Workflows (Recommended for Initial Load)
**Endpoint:** `POST /api/workflow-node-codes/sync`

**Query Parameters:**
- `maxConcurrency` (optional): Number of concurrent API calls (default: 10, max: 50)

**Example Request:**
```http
POST http://localhost:5109/api/workflow-node-codes/sync?maxConcurrency=20
Authorization: Bearer YOUR_JWT_TOKEN
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
Authorization: Bearer YOUR_JWT_TOKEN
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
Authorization: Bearer YOUR_JWT_TOKEN
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

## Authentication

All endpoints require a valid JWT bearer token. Get your token from the `/api/auth/login` endpoint:

```javascript
// 1. Login to get JWT token
async function login(username, password) {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });

  const data = await response.json();

  if (data.success) {
    // Store token for subsequent requests
    localStorage.setItem('token', data.data.token);
    return data.data.token;
  } else {
    throw new Error(data.message || 'Login failed');
  }
}
```

---

## Typical Frontend Flow

### Initial App Load (One-Time Setup)
```javascript
// 1. Sync all workflows on app initialization or admin action
async function syncAllWorkflowNodeCodes() {
  const token = localStorage.getItem('token');

  if (!token) {
    console.error('No authentication token found');
    return;
  }

  try {
    const response = await fetch('/api/workflow-node-codes/sync?maxConcurrency=20', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (response.status === 401) {
      // Token expired or invalid - redirect to login
      window.location.href = '/login';
      return;
    }

    const result = await response.json();
    console.log(`Synced ${result.successCount} workflows`);

    if (result.failureCount > 0) {
      console.warn('Some workflows failed:', result.errors);
    }
  } catch (error) {
    console.error('Sync failed:', error);
  }
}
```

### Workflow Selection (Real-Time)
```javascript
// 2. Get node codes when user selects a workflow
async function getNodeCodesForWorkflow(externalWorkflowId) {
  const token = localStorage.getItem('token');

  try {
    const response = await fetch(
      `/api/workflow-node-codes/${externalWorkflowId}`,
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (response.status === 401) {
      window.location.href = '/login';
      return;
    }

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
// 3. Refresh specific workflow after configuration changes
async function refreshWorkflow(externalWorkflowId) {
  const token = localStorage.getItem('token');

  try {
    const response = await fetch(`/api/workflow-node-codes/sync/${externalWorkflowId}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (response.status === 401) {
      window.location.href = '/login';
      return;
    }

    // Reload node codes
    await getNodeCodesForWorkflow(externalWorkflowId);
  } catch (error) {
    console.error('Refresh failed:', error);
  }
}
```

---

## Important Notes

1. **Authentication Required**: All endpoints require JWT bearer token authentication

2. **ExternalWorkflowId**: This is NOT the local database workflow ID, it's the ID from the external AMR system (stored in `WorkflowDiagram.ExternalWorkflowId`)

3. **Rate Limiting**: The sync endpoint processes requests in parallel with concurrency control to avoid overloading the external API

4. **Data Freshness**: Node codes are cached in the database. Call sync endpoints to refresh data from the external system

5. **Error Handling**: Always check the response for errors and handle failed syncs appropriately

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
