# Ready to Deploy - Quick Instructions

## Files Ready
Your application has been published to: `d:\IPO\IPOClient\IPOClient\publish\`

## Upload These Files to Server

**Destination**: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\`

Upload **ALL** files from the `publish` folder.

## Critical Files to Verify After Upload

1. ✅ `IPOClient.dll` - Main application
2. ✅ `web.config` - IIS configuration (with detailed errors enabled)
3. ✅ `appsettings.json` - Main configuration (already has production DB connection)
4. ✅ `appsettings.Production.json` - Production overrides

## IIS Configuration Checklist

### If Using Subdomain: https://financeapi.ivotiontech.co.in/

1. **Create IIS Site** (not Application):
   - Right-click "Sites" → "Add Website"
   - Site name: `financeapi.ivotiontech.co.in`
   - Physical path: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\`
   - Binding:
     - Type: `https`
     - Port: `443`
     - Host name: `financeapi.ivotiontech.co.in`
     - SSL Certificate: Select your certificate

2. **Configure Application Pool**:
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
   - Identity: ApplicationPoolIdentity (or custom with DB access)

### If Using Path: https://ivotiontech.co.in/financeapi/

1. **Create Application under existing site**:
   - Right-click site → "Add Application"
   - Alias: `financeapi`
   - Physical path: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\`
   - Application Pool: Create new with "No Managed Code"

## Create Logs Folder

On the server, run:
```cmd
cd D:\Inetpub\vhosts\ivotiontech.co.in\financeapi
mkdir logs
icacls logs /grant "IIS_IUSRS:(OI)(CI)F" /T
```

## Testing URLs

After deployment, try these URLs:

1. Swagger UI: https://financeapi.ivotiontech.co.in/swagger
2. Or: https://ivotiontech.co.in/financeapi/swagger
3. API endpoint: https://financeapi.ivotiontech.co.in/api/auth/login

## If You Get Errors

### 500.30 Error (App failed to start)
Check: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\logs\stdout_*.log`

### 404 Error (Not Found)
1. Verify application is configured in IIS (gear icon, not folder icon)
2. Check URL binding/alias matches your access URL
3. Verify IPOClient.dll exists in the folder

### Database Errors
Connection string is already configured in `appsettings.json`:
```
Server=103.191.208.118;Database=IvotionTech;User Id=IvotionTech;Password=*iI@2022;TrustServerCertificate=True;
```

## Configuration Details

### Current web.config settings:
- ✅ Hosting Model: `outofprocess` (better for troubleshooting)
- ✅ Stdout Logging: **Enabled** → `.\logs\stdout`
- ✅ Environment: `Development` (shows detailed errors)
- ✅ Detailed Errors: **Enabled**

### These settings are for DEBUGGING. After it works:
1. Change environment to `Production` in web.config
2. Remove `ASPNETCORE_DETAILEDERRORS` environment variable
3. Change `hostingModel` back to `inprocess` for better performance

## Database Automatic Creation

✅ **The application will automatically create the database and tables on first run!**

### How It Works:
1. On startup, the application checks if the database exists
2. If NOT exists → Creates database from entity models
3. If EXISTS → Checks for pending migrations and applies them
4. Seeds initial data (admin user, type masters, etc.)

### Tables Created Automatically:
- `IPO_UserMasters` - User accounts
- `IPO_TypeMaster` - IPO type lookup
- `IPO_IPOMaster` - IPO records
- `IPO_ApiLogs` - Error logging

### Database Requirements:
- SQL Server user `IvotionTech` must have permissions to CREATE DATABASE
- Connection string is already configured in `appsettings.json`
- Database name: `IvotionTech`

### Manual Migration (Optional - Only if automatic fails):
```bash
# From your local machine
dotnet ef database update --connection "Server=103.191.208.118;Database=IvotionTech;User Id=IvotionTech;Password=*iI@2022;TrustServerCertificate=True;"
```

## Verify .NET Runtime on Server

The server must have **.NET 8.0 Hosting Bundle** installed.

Check with:
```cmd
dotnet --list-runtimes
```

Should show: `Microsoft.AspNetCore.App 8.0.x`

If not installed, download from:
https://dotnet.microsoft.com/download/dotnet/8.0

Install: **ASP.NET Core 8.0 Runtime (Hosting Bundle)**

## Quick Test Commands (On Server)

### Test if app starts manually:
```cmd
cd D:\Inetpub\vhosts\ivotiontech.co.in\financeapi
dotnet IPOClient.dll
```

Should show: `Now listening on: http://localhost:5000`

Press Ctrl+C to stop, then configure in IIS.

### Check database connection:
```cmd
sqlcmd -S 103.191.208.118 -U IvotionTech -P "*iI@2022" -d IvotionTech -Q "SELECT 1"
```

Should show: `1` (successful connection)

## Restart After Changes

After uploading files or changing configuration:
```cmd
iisreset
```

Or restart just the application pool in IIS Manager.

## Expected Behavior

When working correctly:
- ✅ https://financeapi.ivotiontech.co.in/ → Redirects to Swagger UI
- ✅ https://financeapi.ivotiontech.co.in/swagger → Shows API documentation
- ✅ POST /api/auth/login → Returns JWT token

## Features Included

Your deployed application includes:
- ✅ JWT Authentication (15-min access token, 7-day refresh token)
- ✅ Automatic token rotation (refreshes before expiry)
- ✅ Error-only API logging
- ✅ String enum responses ("Success", "Error", "Warning")
- ✅ 24-hour log cleanup
- ✅ Professional RESTful routes
- ✅ HTTP and HTTPS support

## Need Help?

If errors persist:
1. Check stdout logs in `logs\` folder
2. Check Windows Event Viewer → Application logs
3. Verify IIS application pool is running
4. Test database connection
5. Ensure .NET 8.0 runtime is installed

Good luck with deployment! 🚀
