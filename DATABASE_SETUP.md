# Database Setup Guide

## Prerequisites
- SQL Server instance: `DESKTOP-5OB7VRJ\MSSQLSERVER01`
- Database name: `IPO_Ivotiontech`
- Connection verified in `appsettings.json`

## Create Database & Tables

### Step 1: Create Initial Migration
Run this command in the terminal from the project root:
```bash
dotnet ef migrations add InitialCreate
```

### Step 2: Apply Migration to Database
```bash
dotnet ef database update
```

This will create:
- `IPO_UserMasters` table with all required columns

## Create Initial Admin User

### Option 1: Via SQL Server Management Studio
```sql
DECLARE @password NVARCHAR(MAX) = 'Admin@123'

INSERT INTO IPO_UserMasters (
    FName, LName, Email, Password, Phone, IsAdmin, CreatedDate, ExpiryDate
) VALUES (
    'Admin', 
    'User',
    'admin@ivotiontech.com',
    '[Hash will be set by .NET code]',
    '+1234567890',
    1,
    GETUTCDATE(),
    '2027-01-13'
)
```

### Option 2: Via API
1. First, ensure at least one admin user exists (created manually in DB)
2. Login with that admin: `POST /api/auth/login`
3. Use the token to create more users: `POST /api/users`

## Important Notes

- **Password Hashing**: All passwords are hashed using PBKDF2 with SHA256 (10,000 iterations)
- **Email Uniqueness**: Email field has unique constraint
- **Admin Privileges**: Only users with `IsAdmin = 1` can perform CRUD on users
- **Account Expiry**: Checked on every login; expired accounts cannot authenticate

## Verify Installation

Test with REST client:
```
POST /api/auth/login
{
  "email": "admin@ivotiontech.com",
  "password": "Admin@123"
}
```

Should return JWT token and user details.
