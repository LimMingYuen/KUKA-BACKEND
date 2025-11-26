# Permission System Testing Guide

This guide provides comprehensive instructions for testing the newly implemented page access control system.

## Prerequisites

### 1. Start SQL Server
Ensure SQL Server is running and accessible.

### 2. Apply Database Migration
```bash
cd /mnt/c/Users/QYNIX094/source/QES-KUKA-AMR-Penang-Renesas
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20XX_AddPagePermissionSystem'.
Done.
```

### 3. Start the API
```bash
cd /mnt/c/Users/QYNIX094/source/QES-KUKA-AMR-Penang-Renesas
dotnet run --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5109
```

### 4. Access Swagger UI
Open your browser and navigate to:
```
http://localhost:5109/swagger
```

---

## Testing Workflow

### Phase 1: Create Test Data

#### Step 1: Login to Get JWT Token
**Endpoint:** `POST /api/Auth/login`

**Request Body:**
```json
{
  "username": "your_admin_username",
  "password": "your_admin_password"
}
```

**Expected Response:**
```json
{
  "success": true,
  "code": "AUTH_SUCCESS",
  "msg": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-23T10:00:00Z",
    "user": {
      "id": 1,
      "username": "admin",
      "nickname": "Administrator",
      "isSuperAdmin": true,
      "roles": ["ADMIN"],
      "allowedPages": []
    }
  }
}
```

**Important:** Copy the `token` value - you'll need it for all subsequent requests.

#### Step 2: Authorize in Swagger
1. Click the **Authorize** button (lock icon) at the top right of Swagger UI
2. In the "Value" field, enter: `Bearer YOUR_TOKEN_HERE`
3. Click **Authorize**, then **Close**

Now all requests will include your JWT token.

---

### Phase 2: Page Management

#### Test 1: Create Frontend Pages (Auto-Registration Simulation)

**Endpoint:** `POST /api/v1/pages/sync`

**Request Body:**
```json
{
  "pages": [
    {
      "pagePath": "/workflows",
      "pageName": "Workflows",
      "pageIcon": "account_tree"
    },
    {
      "pagePath": "/map-zones",
      "pageName": "Map Zones",
      "pageIcon": "map"
    },
    {
      "pagePath": "/qr-codes",
      "pageName": "QR Codes",
      "pageIcon": "qr_code"
    },
    {
      "pagePath": "/mobile-robots",
      "pageName": "Mobile Robots",
      "pageIcon": "precision_manufacturing"
    },
    {
      "pagePath": "/mission-history",
      "pageName": "Mission History",
      "pageIcon": "history"
    },
    {
      "pagePath": "/user-management",
      "pageName": "User Management",
      "pageIcon": "people"
    },
    {
      "pagePath": "/role-management",
      "pageName": "Role Management",
      "pageIcon": "admin_panel_settings"
    }
  ]
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "totalPages": 7,
    "newPages": 7,
    "updatedPages": 0,
    "unchangedPages": 0
  }
}
```

#### Test 2: Get All Pages

**Endpoint:** `GET /api/v1/pages`

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "pagePath": "/workflows",
      "pageName": "Workflows",
      "pageIcon": "account_tree",
      "createdUtc": "2025-11-22T10:00:00Z"
    },
    // ... more pages
  ]
}
```

#### Test 3: Get Page by ID

**Endpoint:** `GET /api/v1/pages/1`

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "pagePath": "/workflows",
    "pageName": "Workflows",
    "pageIcon": "account_tree",
    "createdUtc": "2025-11-22T10:00:00Z"
  }
}
```

#### Test 4: Get Page by Path

**Endpoint:** `GET /api/v1/pages/path//workflows`

**Note:** Use URL encoding if testing via browser: `/api/v1/pages/path/%2Fworkflows`

**Expected Response:** Same as Test 3

---

### Phase 3: Role Management

#### Test 5: Create Test Roles

**Endpoint:** `POST /api/v1/roles`

**Create "Operator" Role:**
```json
{
  "name": "Operator",
  "roleCode": "OPERATOR",
  "isProtected": false
}
```

**Create "Viewer" Role:**
```json
{
  "name": "Viewer",
  "roleCode": "VIEWER",
  "isProtected": false
}
```

**Expected Response (for each):**
```json
{
  "success": true,
  "data": {
    "id": 2,
    "name": "Operator",
    "roleCode": "OPERATOR",
    "isProtected": false,
    "createdUtc": "2025-11-22T10:05:00Z",
    "updatedUtc": "2025-11-22T10:05:00Z"
  }
}
```

#### Test 6: Get All Roles

**Endpoint:** `GET /api/v1/roles`

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Admin",
      "roleCode": "ADMIN",
      "isProtected": true,
      "createdUtc": "...",
      "updatedUtc": "..."
    },
    {
      "id": 2,
      "name": "Operator",
      "roleCode": "OPERATOR",
      "isProtected": false,
      "createdUtc": "...",
      "updatedUtc": "..."
    }
  ]
}
```

---

### Phase 4: Role Permissions

#### Test 7: Assign Pages to Operator Role (Bulk Set)

**Endpoint:** `POST /api/v1/role-permissions/bulk-set`

**Request Body:**
```json
{
  "roleId": 2,
  "pagePermissions": [
    { "pageId": 1, "canAccess": true },
    { "pageId": 2, "canAccess": true },
    { "pageId": 3, "canAccess": true },
    { "pageId": 4, "canAccess": true },
    { "pageId": 5, "canAccess": true }
  ]
}
```

**Expected Response:**
```json
{
  "success": true,
  "msg": "Bulk set 5 permissions for role.",
  "data": {
    "modifiedCount": 5
  }
}
```

#### Test 8: Assign Pages to Viewer Role (Limited Access)

**Endpoint:** `POST /api/v1/role-permissions/bulk-set`

**Request Body:**
```json
{
  "roleId": 3,
  "pagePermissions": [
    { "pageId": 1, "canAccess": true },
    { "pageId": 5, "canAccess": true }
  ]
}
```

**Expected Response:**
```json
{
  "success": true,
  "msg": "Bulk set 2 permissions for role.",
  "data": {
    "modifiedCount": 2
  }
}
```

#### Test 9: Get Role Permissions Matrix

**Endpoint:** `GET /api/v1/role-permissions/matrix`

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "roles": [
      { "id": 1, "name": "Admin", "roleCode": "ADMIN" },
      { "id": 2, "name": "Operator", "roleCode": "OPERATOR" },
      { "id": 3, "name": "Viewer", "roleCode": "VIEWER" }
    ],
    "pages": [
      { "id": 1, "pagePath": "/workflows", "pageName": "Workflows" },
      { "id": 2, "pagePath": "/map-zones", "pageName": "Map Zones" }
      // ... more pages
    ],
    "permissions": {
      "2_1": true,
      "2_2": true,
      "2_3": true,
      "2_4": true,
      "2_5": true,
      "3_1": true,
      "3_5": true
    }
  }
}
```

**Interpretation:**
- Key format: `{roleId}_{pageId}`
- `"2_1": true` means RoleId=2 (Operator) can access PageId=1 (/workflows)
- Missing keys mean no access

#### Test 10: Get Permissions for Specific Role

**Endpoint:** `GET /api/v1/role-permissions/role/2`

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "roleId": 2,
      "roleName": "Operator",
      "roleCode": "OPERATOR",
      "pageId": 1,
      "pagePath": "/workflows",
      "pageName": "Workflows",
      "canAccess": true,
      "createdUtc": "2025-11-22T10:10:00Z"
    }
    // ... more permissions
  ]
}
```

---

### Phase 5: User Management

#### Test 11: Create Test Users

**Endpoint:** `POST /api/v1/users`

**Create "john_operator" User:**
```json
{
  "username": "john_operator",
  "password": "Test123!",
  "nickname": "John Doe",
  "isSuperAdmin": false,
  "roles": ["OPERATOR"],
  "createBy": "admin",
  "createApp": "KUKA-GUI"
}
```

**Create "jane_viewer" User:**
```json
{
  "username": "jane_viewer",
  "password": "Test123!",
  "nickname": "Jane Smith",
  "isSuperAdmin": false,
  "roles": ["VIEWER"],
  "createBy": "admin",
  "createApp": "KUKA-GUI"
}
```

**Expected Response (for each):**
```json
{
  "success": true,
  "data": {
    "id": 2,
    "username": "john_operator",
    "nickname": "John Doe",
    "isSuperAdmin": false,
    "roles": ["OPERATOR"],
    "createTime": "2025-11-22T10:15:00Z",
    "createBy": "admin",
    "createApp": "KUKA-GUI",
    "lastUpdateTime": "2025-11-22T10:15:00Z"
  }
}
```

---

### Phase 6: User Permissions (Overrides)

#### Test 12: Grant User-Specific Permission Override

**Scenario:** Give "jane_viewer" access to Mobile Robots page (which her role doesn't have)

**Endpoint:** `POST /api/v1/user-permissions`

**Request Body:**
```json
{
  "userId": 3,
  "pageId": 4,
  "canAccess": true
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "userId": 3,
    "username": "jane_viewer",
    "pageId": 4,
    "pagePath": "/mobile-robots",
    "pageName": "Mobile Robots",
    "canAccess": true,
    "createdUtc": "2025-11-22T10:20:00Z"
  }
}
```

#### Test 13: Deny User-Specific Permission Override

**Scenario:** Deny "john_operator" access to Mission History (which his role has)

**Endpoint:** `POST /api/v1/user-permissions`

**Request Body:**
```json
{
  "userId": 2,
  "pageId": 5,
  "canAccess": false
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "id": 2,
    "userId": 2,
    "username": "john_operator",
    "pageId": 5,
    "pagePath": "/mission-history",
    "pageName": "Mission History",
    "canAccess": false,
    "createdUtc": "2025-11-22T10:25:00Z"
  }
}
```

#### Test 14: Bulk Set User Permissions

**Endpoint:** `POST /api/v1/user-permissions/bulk-set`

**Request Body:**
```json
{
  "userId": 3,
  "pagePermissions": [
    { "pageId": 4, "canAccess": true },
    { "pageId": 6, "canAccess": true }
  ]
}
```

**Expected Response:**
```json
{
  "success": true,
  "msg": "Bulk set 2 permissions for user.",
  "data": {
    "modifiedCount": 2
  }
}
```

---

### Phase 7: Permission Checking (Login Test)

#### Test 15: Login as Regular User and Check Permissions

**Endpoint:** `POST /api/Auth/login`

**Login as john_operator:**
```json
{
  "username": "john_operator",
  "password": "Test123!"
}
```

**Expected Response:**
```json
{
  "success": true,
  "code": "AUTH_SUCCESS",
  "msg": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-23T10:30:00Z",
    "user": {
      "id": 2,
      "username": "john_operator",
      "nickname": "John Doe",
      "isSuperAdmin": false,
      "roles": ["OPERATOR"],
      "allowedPages": [
        "/workflows",
        "/map-zones",
        "/qr-codes",
        "/mobile-robots"
      ]
    }
  }
}
```

**Important:** Note that:
- `allowedPages` includes pages from OPERATOR role
- `/mission-history` is NOT included (denied by user permission override in Test 13)

**Login as jane_viewer:**
```json
{
  "username": "jane_viewer",
  "password": "Test123!"
}
```

**Expected Response:**
```json
{
  "success": true,
  "code": "AUTH_SUCCESS",
  "msg": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-23T10:35:00Z",
    "user": {
      "id": 3,
      "username": "jane_viewer",
      "nickname": "Jane Smith",
      "isSuperAdmin": false,
      "roles": ["VIEWER"],
      "allowedPages": [
        "/workflows",
        "/mission-history",
        "/mobile-robots",
        "/user-management"
      ]
    }
  }
}
```

**Important:** Note that:
- `allowedPages` includes pages from VIEWER role (/workflows, /mission-history)
- PLUS user-specific overrides (/mobile-robots, /user-management)

---

## Permission Logic Verification

The system uses this priority for permission checks:

### 1. SuperAdmin Bypass
**Test:** Login as admin (isSuperAdmin: true)
**Expected:** `allowedPages` returns ALL pages (or empty array, since SuperAdmin bypasses checks)

### 2. User Permission Override
**Test:** john_operator denied /mission-history despite role having access
**Expected:** /mission-history NOT in allowedPages

### 3. Role Permissions
**Test:** jane_viewer has VIEWER role with only 2 pages
**Expected:** Those 2 pages in allowedPages

### 4. Default Deny
**Test:** User with no roles and no user permissions
**Expected:** Empty allowedPages array

---

## Error Cases to Test

### Test 16: Duplicate Page Creation
**Endpoint:** `POST /api/v1/pages`

**Request Body:**
```json
{
  "pagePath": "/workflows",
  "pageName": "Duplicate",
  "pageIcon": "error"
}
```

**Expected Response (409 Conflict):**
```json
{
  "title": "Page already exists.",
  "detail": "Page with path '/workflows' already exists.",
  "status": 409,
  "type": "https://httpstatuses.com/409"
}
```

### Test 17: Duplicate Role Permission
**Endpoint:** `POST /api/v1/role-permissions`

**Request Body:**
```json
{
  "roleId": 2,
  "pageId": 1,
  "canAccess": true
}
```

**Expected Response (409 Conflict):**
```json
{
  "title": "Role permission already exists.",
  "detail": "Role permission for RoleId '2' and PageId '1' already exists.",
  "status": 409,
  "type": "https://httpstatuses.com/409"
}
```

### Test 18: Invalid User Permission
**Endpoint:** `POST /api/v1/user-permissions`

**Request Body:**
```json
{
  "userId": 999,
  "pageId": 1,
  "canAccess": true
}
```

**Expected:** Permission created (no FK validation), but won't affect anything

### Test 19: Delete Protected Role
**Endpoint:** `DELETE /api/v1/roles/1` (assuming ADMIN role is protected)

**Expected Response (403 Forbidden):**
```json
{
  "title": "Role is protected.",
  "detail": "Cannot delete protected role 'ADMIN'.",
  "status": 403,
  "type": "https://httpstatuses.com/403"
}
```

---

## Database Verification

After testing, you can verify the data directly in SQL Server:

```sql
-- Check created pages
SELECT * FROM Pages ORDER BY PagePath;

-- Check role permissions
SELECT
    r.Name AS RoleName,
    p.PagePath,
    rp.CanAccess
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.Id
INNER JOIN Pages p ON rp.PageId = p.Id
ORDER BY r.Name, p.PagePath;

-- Check user permissions
SELECT
    u.Username,
    p.PagePath,
    up.CanAccess
FROM UserPermissions up
INNER JOIN Users u ON up.UserId = u.Id
INNER JOIN Pages p ON up.PageId = p.Id
ORDER BY u.Username, p.PagePath;

-- Check all permissions for a user (combined)
DECLARE @UserId INT = 2; -- john_operator

SELECT
    p.PagePath,
    CASE
        WHEN up.CanAccess IS NOT NULL THEN 'User Override: ' + CAST(up.CanAccess AS VARCHAR)
        WHEN rp.CanAccess = 1 THEN 'Role: Allowed'
        ELSE 'Denied'
    END AS PermissionSource
FROM Pages p
LEFT JOIN UserPermissions up ON p.Id = up.PageId AND up.UserId = @UserId
LEFT JOIN Users u ON u.Id = @UserId
LEFT JOIN Roles r ON JSON_VALUE(u.RolesJson, '$[0]') = r.RoleCode
LEFT JOIN RolePermissions rp ON p.Id = rp.PageId AND rp.RoleId = r.Id
ORDER BY p.PagePath;
```

---

## Summary of Test Scenarios

| Test # | Scenario | Expected Result |
|--------|----------|-----------------|
| 1 | Sync 7 frontend pages | All pages created |
| 2-4 | Get pages (all, by ID, by path) | Correct page data returned |
| 5-6 | Create and list roles | OPERATOR and VIEWER roles created |
| 7-8 | Bulk assign role permissions | Permissions created |
| 9-10 | Get permission matrix and by role | Correct permission structure |
| 11 | Create test users | john_operator and jane_viewer created |
| 12-14 | User permission overrides | Overrides created successfully |
| 15 | Login and check allowedPages | Correct pages based on priority logic |
| 16-19 | Error cases | Proper error responses |

---

## Next Steps After Testing

Once backend testing is complete and successful:

1. ✅ Verify all API endpoints work correctly
2. ✅ Confirm permission logic (SuperAdmin → User Override → Role → Deny)
3. ✅ Test error handling
4. 📋 Continue with frontend implementation
5. 📋 Integrate frontend with permission system
6. 📋 Create permission management UI
7. 📋 End-to-end testing

---

## Troubleshooting

**Issue:** Migration fails with "table already exists"
**Solution:** Drop the tables manually or reset the database, then reapply migration

**Issue:** Login returns empty allowedPages for non-SuperAdmin
**Solution:** Verify role permissions are set correctly via GET /api/v1/role-permissions/role/{roleId}

**Issue:** User override not working
**Solution:** Check UserPermissions table directly in SQL, ensure UserId and PageId are correct

**Issue:** SuperAdmin not bypassing permissions
**Solution:** Verify IsSuperAdmin flag in Users table is set to 1 (true)

---

**Testing Checklist:**
- [ ] SQL Server started
- [ ] Migration applied successfully
- [ ] API running on port 5109
- [ ] Swagger UI accessible
- [ ] JWT token obtained from login
- [ ] All Phase 1-7 tests completed
- [ ] Error cases tested
- [ ] Database verification queries run
- [ ] Ready for frontend implementation
