# API Testing Guide - JWT Token Usage

## Quick Start: Getting and Using JWT Token

### Step 1: Login to Get JWT Token

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "email": "admin@ivotiontech.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJhZG1pbkBpdm90aW9udGVjaC5jb20iLCJuYW1lIjoiQWRtaW4gVXNlciIsIklzQWRtaW4iOiJ0cnVlIiwibmJmIjoxNzM2ODE2MDAwLCJleHAiOjE3MzY4MTk2MDAsImlhdCI6MTczNjgxNjAwMCwiaXNzIjoiSVBPQ2xpZW50IiwiYXVkIjoiSVBPQ2xpZW50VXNlcnMifQ.xxx",
    "user": {
      "id": 1,
      "fName": "Admin",
      "lName": "User",
      "email": "admin@ivotiontech.com",
      "phone": "+1234567890",
      "isAdmin": true,
      "createdDate": "2026-01-13T00:00:00"
    }
  },
  "responseType": 1,
  "responseMessage": "Login successful",
  "responseCode": "200",
  "returnId": 1
}
```

### Step 2: Copy the Token

Extract the token value from the response:
```
data.token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Step 3: Use Token in Other API Calls

Add the token to the `Authorization` header as `Bearer <token>`:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Step 4: Example API Call with Token

**Get Users List (Admin only):**
```http
GET https://localhost:7289/api/users?pageNumber=1&pageSize=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Testing in VS Code REST Client

### Method 1: Manual Token Copy-Paste

1. In [IPOClient.http](IPOClient.http) file, run the login request
2. Copy the token from the response
3. Paste it into the variable at the top:
   ```
   @adminToken = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
4. Now use `Authorization: Bearer {{adminToken}}` in other requests

### Method 2: Using Variables in .http File

The file has placeholders:
```
@adminToken = 
@userToken = 
```

Fill these with your tokens after login.

---

## JWT Token Structure

The token includes these claims:
- `sub` (NameIdentifier) - User ID
- `email` - User email
- `name` - Full name (FirstName LastName)
- `IsAdmin` - Admin flag ("true" or "false")
- `exp` - Expiration time (60 minutes by default)
- `iss` - Issuer (IPOClient)
- `aud` - Audience (IPOClientUsers)

---

## Token Expiration

- **Default TTL**: 60 minutes from login
- **After expiration**: Return 401 Unauthorized
- **Solution**: Login again to get a new token

Configure in [appsettings.json](appsettings.json):
```json
"JwtSettings": {
  "ExpirationMinutes": 60
}
```

---

## Common Response Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | Success | Login successful, user retrieved |
| 201 | Created | User created successfully |
| 400 | Bad Request | Missing email/password, email exists |
| 401 | Unauthorized | Invalid credentials, expired token, expired account |
| 403 | Forbidden | Not admin, cannot access resource |
| 404 | Not Found | User not found |
| 500 | Server Error | Database error, unexpected exception |

---

## Response Format

All APIs return this standardized format:

```json
{
  "data": { /* actual response data */ },
  "responseType": 1,  // 1=Success, 2=Error, 3=Warning
  "responseMessage": "Success message or error description",
  "responseCode": "200",  // HTTP status code as string
  "returnId": 123  // Optional: ID of created/updated resource
}
```

---

## Testing Checklist

- [ ] Login and get token
- [ ] Copy token to @adminToken variable
- [ ] Test: Get users list (admin only)
- [ ] Test: Get specific user (admin or own)
- [ ] Test: Create user (admin only)
- [ ] Test: Update user (admin or own)
- [ ] Test: Delete user (admin only)
- [ ] Logout
- [ ] Try expired token (should get 401)
- [ ] Try without token (should get 401)

---

## Troubleshooting

**Q: Getting 401 Unauthorized on protected endpoints?**
- Verify token is included in Authorization header
- Check token hasn't expired (60 minutes)
- Ensure token format is: `Bearer <token_value>`

**Q: Token won't work even when valid?**
- Check request includes: `Authorization: Bearer {{adminToken}}`
- Verify token is from current login (not old)
- Token value shouldn't have extra spaces

**Q: Getting 403 Forbidden on admin-only endpoints?**
- User must have `IsAdmin = true` in database
- Login with admin account to get valid token with `IsAdmin: true` claim

**Q: Need to test as regular user?**
- Login with regular user email/password
- Copy token to `@userToken` variable
- Regular users can only: view own profile, update own phone
