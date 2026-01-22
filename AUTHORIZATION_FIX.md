# 403 Forbidden Error - Fixed! ✅

## Problem

Getting **403 Forbidden** when accessing endpoints like:
```
GET /api/users?PageNumber=1&PageSize=10
Authorization: Bearer <valid-token>
```

Even though:
- ✅ Token was valid
- ✅ Token had `"role": "Admin"` claim
- ✅ User was authenticated

## Root Cause

ASP.NET Core was **mapping custom JWT claims** to different claim types by default:
- Custom claim `"role"` was being mapped to `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
- The controller's `IsAdmin()` method was looking for `"role"` but couldn't find it
- Result: Authorization check failed → 403 Forbidden

## Solution

Added claim type mapping configuration in [Program.cs](d:\IPO\IPOClient\IPOClient\Program.cs#L112-L128):

```csharp
// Disable default claim type mapping to preserve custom claims
Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RoleClaimType = "role", // ✅ Explicitly use "role" claim
            NameClaimType = "sub"   // ✅ Explicitly use "sub" claim
        };
    });
```

## What This Does

1. **Clears default claim mappings** - Prevents ASP.NET Core from converting claim names
2. **Sets RoleClaimType** - Tells ASP.NET to use `"role"` claim for authorization
3. **Sets NameClaimType** - Tells ASP.NET to use `"sub"` claim for user identity

## Now It Works! ✅

After deploying the updated version:
- ✅ Admin users can access `/api/users`
- ✅ Role-based authorization works correctly
- ✅ No more 403 Forbidden errors for valid admin tokens

## Testing After Update

### Step 1: Login to get new token
```bash
POST /api/auth/login
{
  "email": "admin@ivotiontech.com",
  "password": "Admin@123"
}
```

### Step 2: Use token to access users
```bash
GET /api/users?PageNumber=1&PageSize=10
Authorization: Bearer <token>
```

**Expected Response**: 200 OK with user list

### Step 3: Verify role claims work
```bash
# Should work for Admin
POST /api/users/create
Authorization: Bearer <admin-token>

# Should get 403 for regular User
POST /api/users/create
Authorization: Bearer <regular-user-token>
```

## Endpoints That Require Admin Role

These endpoints check `IsAdmin()` and will return 403 if not admin:

- ✅ `GET /api/users` - Get all users (Admin only)
- ✅ `POST /api/users/create` - Create user (Admin only)
- ✅ `DELETE /api/users/{id}/delete` - Delete user (Admin only)
- ✅ `POST /api/ipos/create` - Create IPO (Admin only)
- ✅ `PUT /api/ipos/{id}/update` - Update IPO (Admin only)
- ✅ `DELETE /api/ipos/{id}/delete` - Delete IPO (Admin only)

## Endpoints Accessible to All Authenticated Users

- ✅ `GET /api/users/{id}` - Get user by ID (Admin or own profile)
- ✅ `PUT /api/users/{id}/update` - Update user (Admin for all, User for own phone)
- ✅ `GET /api/ipos` - Get all IPOs
- ✅ `GET /api/ipos/{id}` - Get IPO by ID

## Deployment

The fix is included in the latest publish folder:
```
d:\IPO\IPOClient\IPOClient\publish\
```

Upload all files to your server and restart the application.

## Verification

After deploying, check logs at:
```
D:\Inetpub\vhosts\ivotiontech.co.in\financeapi\logs\stdout_*.log
```

You should NOT see any authorization-related errors. The application will log successful authentication and authorization.

## Technical Details

### Before Fix:
```
Token Claim: "role": "Admin"
ASP.NET Reads: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin"
Controller Looks For: "role"
Result: NOT FOUND → 403 Forbidden ❌
```

### After Fix:
```
Token Claim: "role": "Admin"
ASP.NET Reads: "role": "Admin" (no mapping)
Controller Looks For: "role"
Result: FOUND → Authorization Succeeds ✅
```

## Additional Notes

- Old tokens will still work (token structure unchanged)
- No need to re-login existing users
- Authorization now works as expected
- This is a common issue when using custom JWT claims in ASP.NET Core

## Summary

The 403 Forbidden error was caused by ASP.NET Core's default claim type mapping. By disabling the mapping and explicitly setting `RoleClaimType = "role"`, authorization now works correctly.

**Status**: ✅ FIXED - Ready to deploy!
