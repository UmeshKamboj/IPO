# Automatic Database Creation & Migration

## Overview

Your application is configured to **automatically create and update the database** on startup. No manual SQL scripts or migration commands needed!

## How It Works

### On Application Startup:

```
1. Check if database exists
   ├─ NO → Create database from entities ✅
   │        └─ All tables created automatically
   │
   └─ YES → Check for pending migrations
            ├─ Migrations pending → Apply them ✅
            └─ Up to date → Continue ✅

2. Seed initial data
   └─ Creates admin user, type masters, etc. ✅

3. Start application ✅
```

## Database Configuration

**Connection String** (in appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=103.191.208.118;Database=IvotionTech;User Id=IvotionTech;Password=*iI@2022;TrustServerCertificate=True;"
  }
}
```

## Tables Created Automatically

### 1. IPO_UserMasters
User accounts with authentication details
```
- Id (Primary Key)
- FName, LName, Email, Password
- Phone, IsAdmin, ExpiryDate
- CreatedAt, CreatedBy, etc.
```

### 2. IPO_TypeMaster
IPO type lookup data
```
- Id (Primary Key)
- Name (Open, Close, Upcoming)
- CreatedAt, CreatedBy, etc.
```

### 3. IPO_IPOMaster
IPO records
```
- Id (Primary Key)
- Name, Description, IPOType
- Price, StartDate, EndDate
- CreatedAt, CreatedBy, etc.
```

### 4. IPO_ApiLogs
Error logging for API requests
```
- Id (Primary Key)
- Method, Path, StatusCode
- ErrorMessage, UserId
- RequestTime, DurationMs
```

## Seeded Data

### Default Admin User
```
Email: admin@ivotiontech.com
Password: Admin@123
Role: Admin
```

### IPO Types
```
1. Open
2. Close
3. Upcoming
```

## Database Permissions Required

The SQL Server user must have these permissions:

1. **CREATE DATABASE** - To create database if not exists
2. **CREATE TABLE** - To create tables
3. **ALTER TABLE** - To apply migrations
4. **SELECT, INSERT, UPDATE, DELETE** - For normal operations

### Grant Permissions (if needed):

```sql
USE master;
GO

-- Grant database creation
ALTER SERVER ROLE [dbcreator] ADD MEMBER [IvotionTech];
GO

-- Or grant specific database permissions
USE IvotionTech;
GO
GRANT CREATE TABLE TO [IvotionTech];
GRANT ALTER TO [IvotionTech];
GO
```

## Checking Logs

After deployment, check the stdout logs to verify database creation:

**Log Location**: `D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\logs\stdout_*.log`

### Successful Creation Logs:
```
info: Program[0]
      Checking database existence and applying migrations...
info: Program[0]
      Database created successfully from entities.
info: Program[0]
      Database seeding completed successfully
```

### Existing Database Logs:
```
info: Program[0]
      Checking database existence and applying migrations...
info: Program[0]
      Database is up to date. No migrations needed.
info: Program[0]
      Database seeding completed successfully
```

### Migration Applied Logs:
```
info: Program[0]
      Checking database existence and applying migrations...
info: Program[0]
      Applying 2 pending migrations...
info: Program[0]
      Migrations applied successfully.
info: Program[0]
      Database seeding completed successfully
```

## What If Database Creation Fails?

### Error 1: Permission Denied
**Message**: "CREATE DATABASE permission denied"

**Solution**:
```sql
-- Grant CREATE DATABASE permission
USE master;
ALTER SERVER ROLE [dbcreator] ADD MEMBER [IvotionTech];
```

### Error 2: Database Already Exists
**Message**: "Database 'IvotionTech' already exists"

**Solution**: This is fine! The app will use the existing database and just create missing tables.

### Error 3: Cannot Connect to Database
**Message**: "A network-related or instance-specific error"

**Solution**:
1. Verify SQL Server is accessible from web server
2. Check firewall rules
3. Test connection:
```cmd
sqlcmd -S 103.191.208.118 -U IvotionTech -P "*iI@2022" -Q "SELECT 1"
```

### Error 4: Login Failed
**Message**: "Login failed for user 'IvotionTech'"

**Solution**:
1. Verify user exists in SQL Server
2. Check password is correct
3. Ensure SQL Server authentication is enabled

## Manual Database Creation (Fallback)

If automatic creation fails, you can create manually:

### Step 1: Create Database
```sql
CREATE DATABASE IvotionTech;
GO
```

### Step 2: Run Migrations
```bash
dotnet ef database update --connection "Server=103.191.208.118;Database=IvotionTech;User Id=IvotionTech;Password=*iI@2022;TrustServerCertificate=True;"
```

### Step 3: Restart Application
The app will now seed the data automatically.

## Benefits of Auto-Creation

✅ **Zero Manual Setup** - No SQL scripts to run
✅ **Always Up-to-Date** - Migrations applied automatically
✅ **Self-Healing** - Creates missing tables if dropped
✅ **Development-Friendly** - Same code works locally and in production
✅ **Version Control** - Schema changes tracked in code

## Code Implementation

The auto-creation logic is in [Program.cs](d:\IPO\IPOClient\IPOClient\Program.cs#L171-L220):

```csharp
// Create database if it doesn't exist
bool dbCreated = await db.Database.EnsureCreatedAsync();

if (dbCreated)
{
    logger.LogInformation("Database created successfully from entities.");
}
else
{
    // Database exists, check for pending migrations
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");
    }
}

// Seed initial data
await DatabaseSeeder.SeedAsync(db);
```

## Testing Locally

To test the auto-creation locally:

1. Drop your local database:
```sql
DROP DATABASE IPO_Ivotiontech;
```

2. Run the application:
```bash
dotnet run
```

3. Check logs:
```
info: Checking database existence and applying migrations...
info: Database created successfully from entities.
info: Database seeding completed successfully
```

4. Verify tables exist in SQL Server Management Studio

## Production Deployment

When deploying to production:

1. ✅ Upload all files from `publish` folder
2. ✅ Configure IIS application
3. ✅ Start the application
4. ✅ Database will be created automatically
5. ✅ Check stdout logs to confirm
6. ✅ Test API endpoints

**No manual database setup required!** 🎉

## Important Notes

- The application will NOT drop existing data
- Existing tables are preserved
- Only missing tables/columns are added
- Initial seed data only inserts if not already present
- Safe to restart application multiple times
- Each startup checks and applies any new migrations

## Troubleshooting

If the application starts but database isn't created:

1. Check stdout logs for errors
2. Verify SQL Server connection
3. Check user permissions
4. Look at Windows Event Viewer → Application logs
5. Test database connection manually

If issues persist, the application will still start (database errors don't crash the app), but API calls will fail with database connection errors.
