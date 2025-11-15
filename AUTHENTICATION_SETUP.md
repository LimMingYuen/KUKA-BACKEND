# Authentication Setup Guide

## Overview

This guide explains the dual authentication system implemented for the QES KUKA AMR API:

1. **Internal App Authentication**: Users log in with their own credentials stored in the `Users` table
2. **External API Authentication**: Backend automatically authenticates with external AMR API using default credentials

## Architecture

```
┌─────────────┐
│   Frontend  │
└──────┬──────┘
       │ POST /api/auth/login
       │ {username, password}
       ▼
┌─────────────────────────────────────────┐
│         QES KUKA AMR API                │
│                                          │
│  ┌────────────────────────────────┐    │
│  │  AuthController                │    │
│  │  • Validates user credentials  │    │
│  │  • Issues internal JWT token   │    │
│  │  • Triggers external API login │    │
│  └────────────────────────────────┘    │
│                                          │
│  ┌────────────────────────────────┐    │
│  │  ExternalApiTokenService       │    │
│  │  • Auto-login: admin/Admin     │    │
│  │  • Cache token (1 hour)        │    │
│  │  • Auto-refresh when expired   │    │
│  └────────────────────────────────┘    │
│                                          │
└───────────┬──────────────────────────────┘
            │
            │ (Uses external token for API calls)
            ▼
     ┌──────────────────┐
     │  External AMR API │
     └──────────────────┘
```

## Setup Instructions

### Step 1: Install Dependencies

The required NuGet packages are already added to the project:
- `Microsoft.AspNetCore.Authentication.JwtBearer` (v8.0.5)
- `BCrypt.Net-Next` (v4.0.3)
- `System.IdentityModel.Tokens.Jwt` (v7.6.0)

Run `dotnet restore` to install them:
```bash
dotnet restore
```

### Step 2: Database Migration

Add the `PasswordHash` column to the `Users` table:

```bash
# Create migration
dotnet ef migrations add AddPasswordHashToUser --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Apply migration
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

### Step 3: Create Admin User

You have **three options** to create the default admin user:

#### Option A: Use DbInitializer (Recommended)

Add this to your `Program.cs` before `app.Run()`:

```csharp
// Seed database with admin user (add before app.Run())
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.SeedAsync(context);
}
```

Then run the application once to create the admin user.

#### Option B: Use SQL Script

Run the SQL script located at `/Scripts/AddPasswordHashAndAdminUser.sql`:

**Note**: You'll need to generate a BCrypt hash first. Use this C# code:

```csharp
using QES_KUKA_AMR_API.Utils;

var hash = PasswordHashGenerator.GenerateHash("admin");
Console.WriteLine($"BCrypt hash: {hash}");
```

Then replace the placeholder hash in the SQL script with the generated hash.

#### Option C: Use Register Endpoint

After starting the API, call the register endpoint:

```bash
curl -X POST http://localhost:5109/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin",
    "nickname": "Administrator",
    "roles": ["Admin"]
  }'
```

### Step 4: Configure JWT Secret (IMPORTANT!)

Update the JWT secret key in `appsettings.json` for production:

```json
{
  "Jwt": {
    "SecretKey": "YOUR_SECURE_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS_REQUIRED",
    "Issuer": "QES-KUKA-AMR-API",
    "Audience": "QES-KUKA-AMR-Frontend",
    "ExpirationHours": 24
  }
}
```

**SECURITY WARNING**: The default secret key is for development only. Generate a strong random key for production!

## API Endpoints

### 1. Login (Internal App)

**POST** `/api/auth/login`

Request:
```json
{
  "username": "admin",
  "password": "admin"
}
```

Response:
```json
{
  "success": true,
  "code": "AUTH_SUCCESS",
  "msg": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-16T10:00:00Z",
    "user": {
      "id": 1,
      "username": "admin",
      "nickname": "Administrator",
      "isSuperAdmin": true,
      "roles": ["Admin"]
    }
  }
}
```

### 2. Register (Create New User)

**POST** `/api/auth/register`

Request:
```json
{
  "username": "john.doe",
  "password": "SecurePassword123",
  "nickname": "John Doe",
  "roles": ["Operator"]
}
```

Response: Same as login response

### 3. Get Current User

**GET** `/api/auth/me`

Headers:
```
Authorization: Bearer <your-jwt-token>
```

Response:
```json
{
  "success": true,
  "code": "AUTH_SUCCESS",
  "msg": "Token is valid",
  "data": {
    "id": 1,
    "username": "admin",
    "isSuperAdmin": true,
    "roles": ["Admin"]
  }
}
```

### 4. External Token Status (Debug)

**GET** `/api/auth/external-token-status`

Headers:
```
Authorization: Bearer <your-jwt-token>
```

Response:
```json
{
  "success": true,
  "code": "SUCCESS",
  "msg": "External API token is available",
  "data": {
    "hasToken": true,
    "tokenPreview": "eyJhbGciOi...N8L.kNS"
  }
}
```

## Frontend Integration

### 1. Login Flow

```javascript
// Login request
const response = await fetch('http://localhost:5109/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    username: 'admin',
    password: 'admin'
  })
});

const result = await response.json();

if (result.success) {
  // Store token
  localStorage.setItem('authToken', result.data.token);
  localStorage.setItem('user', JSON.stringify(result.data.user));

  console.log('Login successful!');
  console.log('User:', result.data.user);
}
```

### 2. Making Authenticated Requests

```javascript
// Get stored token
const token = localStorage.getItem('authToken');

// Make API request with token
const response = await fetch('http://localhost:5109/api/missions', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const data = await response.json();
```

### 3. Handle Token Expiration

```javascript
// Check if token is expired
const response = await fetch('http://localhost:5109/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

if (response.status === 401) {
  // Token expired, redirect to login
  localStorage.removeItem('authToken');
  localStorage.removeItem('user');
  window.location.href = '/login';
}
```

### 4. Logout

```javascript
// Clear stored data
localStorage.removeItem('authToken');
localStorage.removeItem('user');
window.location.href = '/login';
```

## How It Works

### Internal App Authentication (24-hour tokens)

1. User enters their username/password in frontend
2. Frontend calls `POST /api/auth/login`
3. Backend validates credentials against `Users` table
4. Backend generates JWT token (valid for 24 hours)
5. Frontend stores token and includes it in all subsequent requests
6. Backend validates token on each request using JWT middleware

### External API Authentication (1-hour auto-refresh)

1. When user logs in, backend triggers `ExternalApiTokenService.GetTokenAsync()`
2. Service checks if it has a valid cached token
3. If token is expired or missing:
   - Service automatically logs in to external API using hardcoded credentials (`admin`/`Admin`)
   - Caches the token for 55 minutes (buffer for 1-hour validity)
4. When any service needs to call external API:
   - Service calls `ExternalApiTokenService.GetTokenAsync()`
   - Gets the cached token (or automatically refreshes if expired)
   - Uses token in external API request
5. Frontend is completely unaware of external API authentication

## Security Considerations

### Production Deployment

1. **Change JWT Secret**: Use a strong, random secret key (min 32 characters)
2. **Change Default Password**: Update admin user password after first login
3. **HTTPS Only**: Use HTTPS in production (currently disabled for IIS HTTP deployment)
4. **CORS Policy**: Restrict allowed origins (currently allows all origins)
5. **Rate Limiting**: Consider adding rate limiting to prevent brute force attacks
6. **Password Policy**: Implement password complexity requirements
7. **External API Credentials**: Store external API credentials securely (consider using environment variables or Azure Key Vault)

### Password Storage

- All passwords are hashed using BCrypt with work factor 11
- BCrypt includes automatic salt generation
- Hashes are 60 characters long
- Never store plaintext passwords

## Troubleshooting

### "Invalid username or password"

- Check that the user exists in the database
- Verify the password is correct
- Check that the `PasswordHash` column exists and has valid BCrypt hashes

### "JWT configuration is missing or invalid"

- Verify `Jwt` section exists in `appsettings.json`
- Ensure `SecretKey` is at least 32 characters
- Check that all required fields (Issuer, Audience, ExpirationHours) are present

### "Failed to obtain external API token"

- Check that the external API (simulator) is running
- Verify `LoginService.LoginUrl` in `appsettings.json` is correct
- Check that the external API accepts `admin`/`Admin` credentials
- Review logs for detailed error messages

### Token expired immediately

- Check system clock is synchronized
- Verify `ExpirationHours` is set correctly in `appsettings.json`
- Check that `ClockSkew` is configured appropriately (currently set to Zero)

## Testing

### Test Login with Swagger

1. Start the API: `dotnet run --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj`
2. Open Swagger: `http://localhost:5109/swagger`
3. Find `POST /api/auth/login` endpoint
4. Click "Try it out"
5. Enter credentials:
   ```json
   {
     "username": "admin",
     "password": "admin"
   }
   ```
6. Click "Execute"
7. Copy the `token` from the response
8. Click "Authorize" button at the top of Swagger page
9. Enter: `Bearer <your-token>`
10. Now you can test protected endpoints

## Default Credentials

**Internal App:**
- Username: `admin`
- Password: `admin`

**External API (Hardcoded in ExternalApiTokenService):**
- Username: `admin`
- Password: `Admin`

**⚠️ SECURITY WARNING**: Change the default admin password in production!

## Additional Resources

- BCrypt.Net-Next Documentation: https://github.com/BcryptNet/bcrypt.net
- JWT.io (Decode tokens): https://jwt.io/
- ASP.NET Core Authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/

## Support

For issues or questions, please check:
1. Application logs in `Logs/` directory
2. Database migration status
3. Configuration in `appsettings.json`
4. Swagger documentation at `/swagger`
