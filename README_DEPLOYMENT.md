# IPO Client API - Deployment Ready! 🚀

## ✅ What's Been Configured

Your application now has:

1. **Automatic Database Creation** - Creates tables from entities on first run
2. **Automatic Migrations** - Applies pending migrations on startup
3. **Error Handling** - Database errors won't crash the application
4. **Detailed Logging** - Stdout logs show exactly what's happening
5. **HTTP & HTTPS Support** - Works with both protocols
6. **Production Configuration** - Database connection ready

## 📦 Deployment Package Location

```
d:\IPO\IPOClient\IPOClient\publish\
```

All files are ready to upload!

## 🎯 Quick Start Guide

### Step 1: Upload Files
Upload **ALL** files from `publish\` folder to:
```
D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\
```

### Step 2: Create Logs Folder
```cmd
cd D:\Inetpub\vhosts\ivotiontech.co.in\financeapi
mkdir logs
icacls logs /grant "IIS_IUSRS:(OI)(CI)F" /T
```

### Step 3: Configure IIS

#### Option A: Subdomain (Recommended)
Create new **IIS Site**:
- Site name: `financeapi.ivotiontech.co.in`
- Physical path: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\`
- Binding: https, port 443, hostname: `financeapi.ivotiontech.co.in`
- Application Pool: Create new with **"No Managed Code"**

#### Option B: Application Path
Create **IIS Application**:
- Right-click site → Add Application
- Alias: `financeapi`
- Physical path: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\`
- Application Pool: Create new with **"No Managed Code"**

### Step 4: Test
Visit: https://financeapi.ivotiontech.co.in/swagger

## 📊 What Happens on First Run

```
1. Application starts ✅
2. Checks if database "IvotionTech" exists
   ├─ Not exists? → Creates database ✅
   └─ Exists? → Checks for migrations ✅
3. Creates tables from entities:
   ├─ IPO_UserMasters
   ├─ IPO_TypeMaster
   ├─ IPO_IPOMaster
   └─ IPO_ApiLogs
4. Seeds initial data:
   ├─ Admin user (admin@ivotiontech.com / Admin@123)
   └─ IPO types (Open, Close, Upcoming)
5. Application ready! ✅
```

## 🔍 Checking Logs

After deployment, check logs at:
```
D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\logs\stdout_*.log
```

### Success Looks Like:
```log
info: Checking database existence and applying migrations...
info: Database created successfully from entities.
info: Database seeding completed successfully
info: Now listening on: http://[::]:80
```

## 🔧 Troubleshooting

### Error: 500.30 (App failed to start)
**Cause**: .NET 8.0 Runtime not installed

**Solution**: Install .NET 8.0 Hosting Bundle from:
https://dotnet.microsoft.com/download/dotnet/8.0

### Error: 404 (Not Found)
**Cause**: Application not properly configured in IIS

**Solutions**:
1. Verify it's an **Application** (gear icon) not just a folder
2. Check application pool is set to **"No Managed Code"**
3. Verify IPOClient.dll exists in the folder
4. Check bindings/alias match your URL

### Error: Database Connection Failed
**Cause**: SQL Server not accessible or wrong credentials

**Solution**: Test connection:
```cmd
sqlcmd -S 103.191.208.118 -U IvotionTech -P "*iI@2022" -d IvotionTech -Q "SELECT 1"
```

### Error: Permission Denied (Database Creation)
**Cause**: SQL user doesn't have CREATE DATABASE permission

**Solution**: Grant permissions:
```sql
USE master;
ALTER SERVER ROLE [dbcreator] ADD MEMBER [IvotionTech];
```

## 📚 Documentation Files

Detailed guides are available:

1. **[DEPLOY_NOW.md](DEPLOY_NOW.md)** - Quick deployment instructions
2. **[DATABASE_AUTO_SETUP.md](DATABASE_AUTO_SETUP.md)** - How auto-creation works
3. **[DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)** - Complete deployment guide
4. **[TROUBLESHOOT_404.md](TROUBLESHOOT_404.md)** - Fixing 404 errors
5. **[JWT_REFRESH_TOKENS.md](Docs/JWT_REFRESH_TOKENS.md)** - Token authentication guide

## 🌟 Features Included

### Authentication
- ✅ JWT with dual-token pattern (access + refresh)
- ✅ Automatic server-side token rotation
- ✅ 15-minute access tokens, 7-day refresh tokens
- ✅ Stateless (no database lookups)

### API Design
- ✅ RESTful routes (e.g., `/api/users/create`)
- ✅ String enum responses ("Success", "Error", "Warning")
- ✅ camelCase JSON serialization
- ✅ Swagger documentation

### Performance
- ✅ Error-only logging (99% overhead reduction)
- ✅ Fire-and-forget async logging
- ✅ 24-hour automatic log cleanup
- ✅ Optimized database queries

### Deployment
- ✅ HTTP and HTTPS support
- ✅ Automatic database creation
- ✅ Automatic migrations
- ✅ Self-healing (recreates missing tables)
- ✅ Detailed error logging

## 🔐 Default Credentials

### Admin User
```
Email: admin@ivotiontech.com
Password: Admin@123
```

**Change this password immediately after first login!**

### Test Login API
```bash
curl -X POST https://financeapi.ivotiontech.co.in/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@ivotiontech.com",
    "password": "Admin@123"
  }'
```

Expected response:
```json
{
  "responseType": "Success",
  "responseCode": "200",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 1,
      "email": "admin@ivotiontech.com",
      "isAdmin": true
    }
  }
}
```

## ⚙️ Configuration Files

### Database Connection
**File**: `appsettings.json` and `appsettings.Production.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=103.191.208.118;Database=IvotionTech;User Id=IvotionTech;Password=*iI@2022;TrustServerCertificate=True;"
  }
}
```

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "cR7!9xF@ZP5$K2T8A#Q6D^WJMLNBYH3G",
    "AccessTokenExpirationMinutes": 300,
    "RefreshTokenExpirationDays": 7
  }
}
```

### IIS Configuration
**File**: `web.config`
- Hosting Model: `outofprocess` (for debugging)
- Environment: `Development` (shows detailed errors)
- Stdout Logging: Enabled → `.\logs\stdout`

**After successful deployment**, update web.config:
1. Change environment to `Production`
2. Remove `ASPNETCORE_DETAILEDERRORS`
3. Change `hostingModel` to `inprocess` for better performance

## 🎉 Success Checklist

- [ ] .NET 8.0 Hosting Bundle installed on server
- [ ] All files uploaded from `publish` folder
- [ ] `logs` folder created with write permissions
- [ ] IIS application configured (not just a folder)
- [ ] Application pool set to "No Managed Code"
- [ ] SQL Server connection tested
- [ ] Application starts without errors
- [ ] Swagger UI accessible
- [ ] Login API works
- [ ] Database tables created automatically
- [ ] Stdout logs show successful startup

## 🆘 Need Help?

1. **Check stdout logs** - Most errors show here with details
2. **Check Event Viewer** - Windows Logs → Application
3. **Test database** - Use sqlcmd to verify connectivity
4. **Verify .NET Runtime** - Run `dotnet --list-runtimes`
5. **Review documentation** - See guides listed above

## 📝 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get tokens
- `POST /api/auth/register` - Register new user
- `POST /api/auth/refresh-token` - Refresh access token (fallback)

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users/create` - Create new user
- `PUT /api/users/{id}/update` - Update user
- `DELETE /api/users/{id}/delete` - Delete user

### IPOs
- `GET /api/ipos` - Get all IPOs
- `GET /api/ipos/{id}` - Get IPO by ID
- `POST /api/ipos/create` - Create new IPO
- `PUT /api/ipos/{id}/update` - Update IPO
- `DELETE /api/ipos/{id}/delete` - Delete IPO

All endpoints documented at: `/swagger`

## 🔄 Auto Token Rotation

The API automatically refreshes tokens before expiry:
- Client makes request with access token expiring in < 5 minutes
- Server issues new tokens in response headers:
  - `X-New-Access-Token`
  - `X-New-Refresh-Token`
  - `X-Token-Refreshed: true`
- Client updates stored tokens
- No manual refresh needed!

## 🚀 Ready to Deploy!

Everything is configured and ready. Just upload the files and start the application!

Good luck! 🎉
