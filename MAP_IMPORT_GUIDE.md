# Map Import API - Guide

This guide explains how to use the new Map Import API to load QR code coordinates from JSON files into the database.

## Overview

The Map Import API allows you to:
- Import QR code positions and navigation parameters from KUKA AMR map JSON files
- Preview map data before importing
- Get statistics about map structure
- Update existing QR codes or skip duplicates

## What Was Added

### 1. Database Changes (QrCode Entity)

New fields added to the `QrCode` table:
- `XCoordinate` - X position on map (nullable double)
- `YCoordinate` - Y position on map (nullable double)
- `NodeUuid` - Unique node identifier from map system
- `NodeType` - Type of node (1=standard, etc.)
- `TransitOrientations` - Allowed orientations (e.g., "0,180")
- `DistanceAccuracy` - Navigation distance accuracy in meters
- `GoalDistanceAccuracy` - Goal position accuracy
- `AngularAccuracy` - Angular accuracy in degrees
- `GoalAngularAccuracy` - Goal angular accuracy
- `SpecialConfig` - JSON string for special configurations
- `FunctionListJson` - JSON string for node functions (charging, container handling, etc.)

### 2. New Services

**MapImportService** (`Services/MapImport/MapImportService.cs`)
- Parses map JSON files
- Extracts QR code coordinates and navigation parameters
- Imports data into QrCode table
- Handles updates and duplicate detection

### 3. New API Endpoints

**MapImportController** (`/api/mapimport`)

#### POST /api/mapimport/import
Import QR codes from JSON file into database.

**Request:**
```json
{
  "filePath": "/path/to/map_Sim1_1_4u1.json",
  "overwriteExisting": false
}
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully imported 62 nodes, updated 0 nodes from map 'Sim1'",
  "stats": {
    "mapCode": "Sim1",
    "totalNodesInFile": 62,
    "nodesImported": 62,
    "nodesUpdated": 0,
    "nodesSkipped": 0,
    "nodesFailed": 0,
    "importedAt": "2025-11-16T10:30:00Z"
  },
  "errors": [],
  "warnings": []
}
```

#### GET /api/mapimport/preview?filePath=/path/to/map.json
Preview map data without importing.

Returns the complete parsed map structure.

#### GET /api/mapimport/statistics?filePath=/path/to/map.json
Get statistics about a map file.

**Response:**
```json
{
  "mapCode": "Sim1",
  "totalFloors": 1,
  "floors": [
    {
      "floorLevel": 1,
      "floorName": "1",
      "dimensions": {
        "length": 115.2,
        "width": 64.8
      },
      "totalNodes": 62,
      "totalEdges": 164,
      "rackParkingNodes": 17,
      "nodesWithFunctions": 26,
      "coordinateRange": {
        "xMin": 12.96,
        "xMax": 54.388,
        "yMin": 6.256,
        "yMax": 99.665
      }
    }
  ]
}
```

#### GET /api/mapimport/sync-status?mapCode=Sim1
Check sync status between external API data and JSON coordinate data.

**Query Parameters:**
- `mapCode` (optional) - Filter by specific map code

**Response:**
```json
{
  "summary": {
    "totalQrCodes": 100,
    "withCoordinates": 62,
    "withoutCoordinates": 38,
    "coordinateCoverage": 62.0,
    "partialDataCount": 0
  },
  "byMapCode": [
    {
      "mapCode": "Sim1",
      "total": 62,
      "withCoordinates": 62,
      "withoutCoordinates": 0,
      "coveragePercent": 100.0
    },
    {
      "mapCode": "Sim2",
      "total": 38,
      "withCoordinates": 0,
      "withoutCoordinates": 38,
      "coveragePercent": 0.0
    }
  ],
  "qrCodesWithCoordinates": [
    {
      "nodeLabel": "RackPark1",
      "mapCode": "Sim1",
      "coordinates": { "x": 19.003, "y": 49.548 },
      "nodeUuid": "Sim1-1-1",
      "hasExternalId": true,
      "hasFunctions": true
    }
  ],
  "qrCodesMissingCoordinates": [
    {
      "nodeLabel": "SomeNode",
      "mapCode": "Sim2",
      "floorNumber": "1",
      "hasExternalId": true,
      "reliability": 95,
      "lastUpdate": "2025-11-16 10:30:00"
    }
  ],
  "warnings": [
    {
      "type": "MISSING_COORDINATES",
      "message": "38 QR code(s) are missing coordinate data",
      "recommendation": "Import coordinates from map JSON file using /api/mapimport/import"
    }
  ]
}
```

**Use Cases:**
- Check data completeness after syncing from external API
- Identify which QR codes need coordinate data
- Verify import success
- Monitor coordinate coverage across multiple maps

## Setup Instructions

### 1. Run Database Migration

First, you need to add the new columns to the QrCode table:

```bash
# Navigate to project directory
cd /path/to/KUKA-BACKEND

# Create migration
dotnet ef migrations add AddQrCodeCoordinatesAndNavigation --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj --output-dir Data/Migrations

# Apply migration to database
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

### 2. Build and Run

```bash
# Build the solution
dotnet build QES-KUKA-AMR.sln

# Run the API
dotnet run --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

The API will start on `http://localhost:5109`

### 3. Test the API

#### Using Swagger UI

1. Navigate to `http://localhost:5109/swagger`
2. Authorize using JWT token (if required)
3. Try the `/api/mapimport/statistics` endpoint first to preview the data
4. Use `/api/mapimport/import` to import into database

#### Using cURL

**Preview statistics:**
```bash
curl -X GET "http://localhost:5109/api/mapimport/statistics?filePath=/home/user/KUKA-BACKEND/map_Sim1_1_4u1.json"
```

**Import data:**
```bash
curl -X POST "http://localhost:5109/api/mapimport/import" \
  -H "Content-Type: application/json" \
  -d '{
    "filePath": "/home/user/KUKA-BACKEND/map_Sim1_1_4u1.json",
    "overwriteExisting": false
  }'
```

## Usage Examples

### Example 1: Complete Integration Workflow

```bash
# Step 1: Sync QR codes from external API
curl -X POST "http://localhost:5109/api/qrcodes/sync"

# Step 2: Check sync status (see what's missing coordinates)
curl -X GET "http://localhost:5109/api/mapimport/sync-status"

# Step 3: Preview map file statistics
curl -X GET "http://localhost:5109/api/mapimport/statistics?filePath=/path/to/map.json"

# Step 4: Import coordinates from JSON file
curl -X POST "http://localhost:5109/api/mapimport/import" \
  -H "Content-Type: application/json" \
  -d '{
    "filePath": "/path/to/map.json",
    "overwriteExisting": true
  }'

# Step 5: Verify coordinate coverage
curl -X GET "http://localhost:5109/api/mapimport/sync-status"
```

### Example 2: Import New Map

```bash
# First, preview the statistics
curl -X GET "http://localhost:5109/api/mapimport/statistics?filePath=/path/to/map.json"

# If statistics look good, import
curl -X POST "http://localhost:5109/api/mapimport/import" \
  -H "Content-Type: application/json" \
  -d '{
    "filePath": "/path/to/map.json",
    "overwriteExisting": false
  }'

# Check what was imported
curl -X GET "http://localhost:5109/api/mapimport/sync-status?mapCode=Sim1"
```

### Example 3: Update Existing Map Data

Use `overwriteExisting: true` to update coordinates of existing QR codes:

```bash
curl -X POST "http://localhost:5109/api/mapimport/import" \
  -H "Content-Type: application/json" \
  -d '{
    "filePath": "/path/to/updated_map.json",
    "overwriteExisting": true
  }'

# Verify the update worked
curl -X GET "http://localhost:5109/api/mapimport/sync-status?mapCode=Sim1"
```

### Example 4: Monitor Coverage Across Multiple Maps

```bash
# Get overall sync status across all maps
curl -X GET "http://localhost:5109/api/mapimport/sync-status"

# Check specific map
curl -X GET "http://localhost:5109/api/mapimport/sync-status?mapCode=Sim1"
curl -X GET "http://localhost:5109/api/mapimport/sync-status?mapCode=Sim2"
```

### Example 5: Preview Full Map Structure

```bash
curl -X GET "http://localhost:5109/api/mapimport/preview?filePath=/path/to/map.json" > map_preview.json
```

## Map File Structure

The API expects JSON files in KUKA AMR map format:

```json
{
  "mapCode": "Sim1",
  "floorList": [
    {
      "floorLevel": 1,
      "floorName": "1",
      "floorNumber": "1",
      "floorLength": 115.2,
      "floorWidth": 64.8,
      "nodeList": [
        {
          "nodeLabel": "RackPark1",
          "nodeNumber": 1,
          "nodeUuid": "Sim1-1-1",
          "xCoordinate": 19.003,
          "yCoordinate": 49.548,
          "transitOrientations": "0,180",
          "functionList": [...]
        }
      ],
      "edgeList": [...]
    }
  ]
}
```

## Querying Imported Data

After import, you can query the QR codes with coordinates:

```sql
-- Get all QR codes with coordinates
SELECT NodeLabel, MapCode, XCoordinate, YCoordinate, TransitOrientations
FROM QrCodes
WHERE XCoordinate IS NOT NULL;

-- Get rack parking locations
SELECT NodeLabel, XCoordinate, YCoordinate
FROM QrCodes
WHERE NodeLabel LIKE 'RackPark%'
ORDER BY NodeLabel;

-- Get nodes with functions
SELECT NodeLabel, XCoordinate, YCoordinate, FunctionListJson
FROM QrCodes
WHERE FunctionListJson IS NOT NULL;
```

## Troubleshooting

### Issue: File Not Found
**Error:** "File not found or invalid JSON format"

**Solution:** Ensure the file path is absolute and the file exists:
```bash
ls -la /path/to/map.json
```

### Issue: Duplicate Nodes Skipped
**Warning:** "Node 'RackPark1' already exists, skipped"

**Solution:** Use `overwriteExisting: true` to update existing records.

### Issue: Permission Denied
**Error:** Cannot read file

**Solution:** Ensure the API process has read permissions on the JSON file:
```bash
chmod 644 /path/to/map.json
```

## Integration with Existing System

The imported QR code coordinates can be used for:

1. **Mission Planning** - Validate mission waypoints against actual map positions
2. **Workflow Visualization** - Display workflow paths on map with coordinates
3. **Robot Tracking** - Compare robot positions with QR code locations
4. **Analytics** - Analyze travel distances and path efficiency
5. **UI Map Display** - Render map layouts with QR code positions

## File Locations

- **DTOs:** `QES-KUKA-AMR-API/Models/MapImport/`
- **Service:** `QES-KUKA-AMR-API/Services/MapImport/MapImportService.cs`
- **Controller:** `QES-KUKA-AMR-API/Controllers/MapImportController.cs`
- **Entity:** `QES-KUKA-AMR-API/Data/Entities/QrCode.cs`
- **Test Map:** `/home/user/KUKA-BACKEND/map_Sim1_1_4u1.json`

## Next Steps

1. Run the database migration
2. Test the import with `map_Sim1_1_4u1.json`
3. Verify imported data in database
4. Integrate coordinate data with frontend map visualization
5. Update mission validation to check against map coordinates
