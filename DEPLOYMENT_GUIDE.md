# IIS Deployment Guide for IPO Client API

## Prerequisites

1. **.NET 8.0 Runtime** must be installed on the server
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install: **ASP.NET Core Runtime 8.0.x (Hosting Bundle)**

2. **IIS with ASP.NET Core Module V2**
   - The Hosting Bundle installer includes this module

## Common Causes of HTTP Error 500.30

### 1. Missing .NET Runtime
**Solution**: Install .NET 8.0 Hosting Bundle from Microsoft

### 2. Database Connection Issues
**Solution**: Update connection string in `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=IPO_Ivotiontech;User Id=USERNAME;Password=PASSWORD;TrustServerCertificate=true;"
  }
}
```

### 3. Missing Dependencies
**Solution**: Ensure all DLL files are published together

### 4. Incorrect Application Pool Settings
**Solution**:
- .NET CLR Version: **No Managed Code**
- Managed Pipeline Mode: Integrated

### 5. Permissions Issues
**Solution**: Grant IIS_IUSRS read/execute permissions on the application folder

## Deployment Steps

### Step 1: Publish the Application

```bash
dotnet publish -c Release -o ./publish
```

### Step 2: Update Configuration

1. Edit `publish/appsettings.Production.json` with your production database connection string
2. Ensure `web.config` is present in the publish folder

### Step 3: Create Logs Directory

On the server at `https://financeapi.ivotiontech.co.in/`:
```bash
mkdir logs
```

Grant write permissions to IIS_IUSRS:
```bash
icacls logs /grant "IIS_IUSRS:(OI)(CI)F" /T
```

### Step 4: Upload Files

Upload all files from the `publish` folder to:
```
D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\
```

### Step 5: Configure IIS Application Pool

1. Create or select application pool
2. Set **.NET CLR Version**: **No Managed Code**
3. Set **Identity**: ApplicationPoolIdentity (or custom account with DB access)

### Step 6: Create IIS Application

1. Open IIS Manager
2. Right-click on site → Add Application
3. Alias: `financeapi` (or map to subdomain)
4. Application Pool: Select the pool from Step 5
5. Physical Path: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\`

### Step 7: Verify Logs

After attempting to access the site, check these locations for error details:

1. **Application Logs**: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\logs\stdout_*.log`
2. **Windows Event Viewer**: Application logs
3. **IIS Logs**: `C:\inetpub\logs\LogFiles\`

## Troubleshooting

### View Detailed Error Messages

The current `web.config` is configured with:
- `ASPNETCORE_DETAILEDERRORS=true`
- `ASPNETCORE_ENVIRONMENT=Development`
- Detailed error mode enabled

Visit the site again and you should see detailed error information.

### Check Stdout Logs

Navigate to the `logs` folder and open the most recent `stdout_*.log` file:
```bash
cd D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\logs
type stdout_*.log
```

### Common Issues and Solutions

#### "Failed to start application"
- Check if .NET 8.0 Runtime is installed: `dotnet --list-runtimes`
- Verify all DLL files are present

#### "Unable to connect to database"
- Test database connection string
- Check firewall rules
- Verify SQL Server allows remote connections
- Check SQL Server authentication mode (Windows vs SQL)

#### "Access Denied" errors
- Grant permissions: `icacls "D:\Inetpub\vhosts\ivotiontech.co.in\financeapi" /grant "IIS_IUSRS:(OI)(CI)RX" /T`
- Ensure logs folder has write permissions

#### "Module not found"
- Reinstall .NET 8.0 Hosting Bundle
- Restart IIS: `iisreset`

## Database Setup

### Run Migrations

Before deploying, ensure database is ready:

```bash
# Create database if not exists
# Run this SQL on your production SQL Server:
CREATE DATABASE IPO_Ivotiontech;
```

Then run migrations from your local machine pointing to production:
```bash
dotnet ef database update --connection "Server=PROD_SERVER;Database=IPO_Ivotiontech;User Id=USER;Password=PASS;"
```

Or run the migration SQL scripts manually from the `Migrations` folder.

## Security Considerations (Post-Deployment)

Once the application is working, update `web.config`:

1. Change environment to Production:
```xml
<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
```

2. Remove detailed errors:
```xml
<!-- Remove this line -->
<environmentVariable name="ASPNETCORE_DETAILEDERRORS" value="true" />
```

3. Disable detailed HTTP errors:
```xml
<!-- Remove or change to Custom -->
<httpErrors errorMode="Custom" />
```

## Restart the Application

After any configuration changes:
```bash
iisreset
```

Or restart just the application pool in IIS Manager.

## Quick Checklist

- [ ] .NET 8.0 Hosting Bundle installed
- [ ] Application published to folder
- [ ] `appsettings.Production.json` updated with correct connection string
- [ ] `logs` folder created with write permissions
- [ ] Application pool set to "No Managed Code"
- [ ] IIS application configured correctly
- [ ] Database exists and migrations applied
- [ ] Checked stdout logs for errors
- [ ] Tested database connection from server

## Support

If issues persist after following this guide, check the stdout logs in the `logs` folder for specific error messages.
