# SMTP Email Notification Setup Guide

This guide explains how to configure and use the SMTP email notification feature for mission API errors.

## Overview

The system sends email notifications when the following APIs encounter errors:
- **Submit Mission API** (`POST /api/missions/submit`)
- **Job Query API** (`POST /api/missions/jobs/query`)

Notifications are sent to configured recipients and include detailed error reports with request/response data, stack traces, and system information.

---

## Setup Instructions

### Step 1: Apply Database Migration

Run the following command to create the `EmailRecipients` table:

```bash
dotnet ef database update --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

### Step 2: Configure SMTP Settings

Edit `appsettings.json` and configure the SMTP section:

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "your-smtp-username",
  "Password": "your-smtp-password",
  "FromAddress": "kuka-amr@example.com",
  "FromDisplayName": "KUKA AMR System",
  "EnableSsl": true,
  "TimeoutMs": 30000,
  "Enabled": true
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `Host` | SMTP server hostname | - |
| `Port` | SMTP server port | 587 |
| `Username` | SMTP authentication username | - |
| `Password` | SMTP authentication password | - |
| `FromAddress` | Sender email address | - |
| `FromDisplayName` | Sender display name | "KUKA AMR System" |
| `EnableSsl` | Use SSL/TLS encryption | true |
| `TimeoutMs` | Connection timeout in milliseconds | 30000 |
| `Enabled` | Enable/disable email sending | false |

> **Note:** Set `Enabled` to `true` to activate email notifications.

### Step 3: Add Email Recipients

1. Log in to the KUKA GUI application
2. Navigate to **Administration > Email Recipients**
3. Click **Add Recipient** to create a new recipient
4. Fill in the required fields:
   - **Email Address**: The recipient's email address
   - **Display Name**: A friendly name for the recipient
   - **Description** (optional): Notes about this recipient
   - **Notification Types**: Select which error types trigger notifications
     - `Mission Submit Error` - Errors when submitting missions
     - `Job Query Error` - Errors when querying job status
   - **Active**: Toggle to enable/disable this recipient

### Step 4: Test the Configuration

1. From the Email Recipients page, click the **Send** icon on any recipient row
2. Confirm the test email dialog
3. Check the recipient's inbox for the test email

If the test email is received successfully, the SMTP configuration is working correctly.

---

## How It Works

### Error Notification Flow

```
1. API Error Occurs (Submit Mission or Job Query)
           ↓
2. Error is logged and returned to client
           ↓
3. Fire-and-forget notification triggered (does not block API response)
           ↓
4. ErrorNotificationService queries active recipients
           ↓
5. HTML email generated with error details
           ↓
6. Email sent to all matching recipients
```

### Email Content

Error notification emails include:

- **Error Summary**: Type, message, and timestamp
- **Request Details**: URL and request body (JSON formatted)
- **Response Details**: HTTP status code and response body (if available)
- **Stack Trace**: Full exception stack trace
- **System Information**: Server name and UTC timestamp

### Notification Types

Recipients can subscribe to specific notification types:

| Type | Triggered By |
|------|--------------|
| `MissionError` | Errors in Submit Mission API |
| `JobQueryError` | Errors in Job Query API |

Only active recipients with matching notification types receive emails.

---

## API Endpoints

The Email Recipients API is available at `/api/v1/email-recipients`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get all recipients |
| GET | `/{id}` | Get recipient by ID |
| POST | `/` | Create new recipient |
| PUT | `/{id}` | Update recipient |
| DELETE | `/{id}` | Delete recipient |
| POST | `/test-email` | Send test email |

### Example: Create Recipient

```bash
curl -X POST "http://localhost:5109/api/v1/email-recipients" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "emailAddress": "admin@example.com",
    "displayName": "System Admin",
    "description": "Primary admin contact",
    "notificationTypes": "MissionError,JobQueryError",
    "isActive": true
  }'
```

### Example: Send Test Email

```bash
curl -X POST "http://localhost:5109/api/v1/email-recipients/test-email" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "testEmailAddress": "test@example.com"
  }'
```

---

## Troubleshooting

### Emails Not Being Sent

1. **Check SMTP is enabled**: Verify `Smtp.Enabled` is `true` in `appsettings.json`
2. **Check recipient is active**: Ensure the recipient's `IsActive` flag is `true`
3. **Check notification types**: Verify the recipient has the correct notification types selected
4. **Check SMTP credentials**: Verify username/password are correct
5. **Check firewall**: Ensure the SMTP port is accessible from the server

### Test Email Fails

1. **Check server logs**: Look for SMTP connection errors in the application logs
2. **Verify SSL settings**: Some servers require `EnableSsl: false` for port 25
3. **Check authentication**: Some servers require app-specific passwords

### Common SMTP Configurations

**Gmail (with App Password):**
```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "EnableSsl": true,
  "Enabled": true
}
```

**Microsoft 365:**
```json
"Smtp": {
  "Host": "smtp.office365.com",
  "Port": 587,
  "Username": "your-email@company.com",
  "Password": "your-password",
  "EnableSsl": true,
  "Enabled": true
}
```

**Internal SMTP Relay (no auth):**
```json
"Smtp": {
  "Host": "mail.internal.company.com",
  "Port": 25,
  "Username": "",
  "Password": "",
  "EnableSsl": false,
  "Enabled": true
}
```

---

## Security Considerations

1. **Store SMTP credentials securely**: Consider using environment variables or Azure Key Vault for production
2. **Use TLS/SSL**: Always enable SSL when connecting over the internet
3. **Limit recipient access**: Only administrators should manage email recipients
4. **Review email content**: Error emails may contain sensitive request data

---

## Files Reference

### Backend Files

| File | Description |
|------|-------------|
| `Options/SmtpOptions.cs` | SMTP configuration class |
| `Data/Entities/EmailRecipient.cs` | Database entity |
| `Models/EmailRecipients/EmailRecipientDtos.cs` | Request/response DTOs |
| `Services/Email/SmtpEmailService.cs` | Email sending implementation |
| `Services/ErrorNotification/ErrorNotificationService.cs` | Error notification logic |
| `Services/EmailRecipients/EmailRecipientService.cs` | CRUD service |
| `Controllers/EmailRecipientsController.cs` | REST API controller |

### Frontend Files

| File | Description |
|------|-------------|
| `models/email-recipients.models.ts` | TypeScript interfaces |
| `services/email-recipients.service.ts` | API service |
| `pages/email-recipients/` | Page component and configuration |
