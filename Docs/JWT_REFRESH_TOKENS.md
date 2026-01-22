# JWT Refresh Token Implementation

## Overview
Secure, stateless JWT refresh token system with automatic token rotation. No database required - everything is handled via JWT claims.

## Token Lifetimes

| Token Type | Lifetime | Purpose |
|------------|----------|---------|
| **Access Token** | 15 minutes | API access, short-lived for security |
| **Refresh Token** | 7 days | Generate new access tokens, long-lived |

This follows OAuth 2.0 best practices for security.

## Why Short Access Tokens?

### Security Benefits
1. **Limited exposure**: If stolen, only valid for 15 minutes
2. **Quick revocation**: User changes propagate within 15 minutes
3. **Reduced attack window**: Minimal time for replay attacks
4. **Compliance**: Meets security standards (PCI-DSS, SOC2)

### Why Not Longer?
- ❌ 24-hour tokens: Too long if credentials compromised
- ❌ Impossible to revoke until expiry
- ❌ User permission changes delayed
- ❌ Fails security audits

## How It Works

### 1. Login Flow
```
Client                    Server
  |                         |
  |--  POST /api/auth/login -->
  |     {email, password}   |
  |                         |
  |<-- 200 OK --------------|
  |   {                     |
  |     token: "access",    | ← 15 min
  |     refreshToken: "ref" | ← 7 days
  |   }                     |
```

### 2. API Request Flow
```
Client                    Server
  |                         |
  |--  GET /api/users ------|
  |   Authorization:        |
  |   Bearer <access-token> |
  |                         |
  |<-- 200 OK --------------|
  |   { data }              |
```

### 3. Token Refresh Flow (Token Rotation)
```
Client                    Server
  |                         |
  |-- POST /api/auth/refresh-token -->
  |  Authorization:         |
  |  Bearer <refresh-token> |
  |                         |
  |<-- 200 OK --------------|
  |   {                     |
  |     token: "new-access",     | ← New 15 min
  |     refreshToken: "new-ref"  | ← New 7 days
  |   }                     |
  |                         |
  | ⚠️ Old refresh token    |
  |    is now invalid!      |
```

## Token Rotation Security

### What is Token Rotation?
Each time you use a refresh token, you get:
- New access token (15 min)
- **New refresh token (7 days)**

The old refresh token becomes invalid immediately.

### Why Rotation?
1. **Prevents replay attacks**: Stolen refresh tokens can only be used once
2. **Detects theft**: If both client and attacker use the same refresh token, you know it's compromised
3. **Automatic revocation**: Old tokens can't be reused
4. **Industry standard**: OAuth 2.0 BCP (Best Current Practice)

## Token Structure

### Access Token Claims
```json
{
  "sub": "1",           // User ID
  "email": "user@example.com",
  "role": "Admin",      // or "User"
  "cid": "0",           // Company ID
  "type": "access",     // Token type
  "exp": 1234567890,    // Expires in 15 min
  "iss": "IPOClient",
  "aud": "IPOClientUsers"
}
```

### Refresh Token Claims
```json
{
  "sub": "1",           // User ID (minimal claims)
  "type": "refresh",    // Token type
  "exp": 1234567890,    // Expires in 7 days
  "iss": "IPOClient",
  "aud": "IPOClientUsers"
}
```

## API Endpoints

### POST /api/auth/login
Authenticate and receive tokens.

**Request:**
```json
{
  "email": "admin@test.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 1,
      "fName": "Admin",
      "lName": "User",
      "email": "admin@test.com",
      "isAdmin": true
    }
  },
  "responseType": "Success",
  "responseMessage": "Login successful",
  "responseCode": "200"
}
```

### POST /api/auth/refresh-token
Get new access token using refresh token.

**Request:**
```
Authorization: Bearer <refresh-token>
```

**Response:**
```json
{
  "data": {
    "token": "eyJhbGc...",      // New access token
    "refreshToken": "eyJhbGc...", // New refresh token (rotated!)
    "user": { ... }
  },
  "responseType": "Success",
  "responseMessage": "Token refreshed successfully",
  "responseCode": "200"
}
```

**Important**: The old refresh token is now invalid!

## Automatic Server-Side Token Rotation

The API includes automatic token rotation middleware that refreshes tokens **before** they expire.

### How It Works

1. **Automatic Detection**: Middleware checks all authenticated requests
2. **Proactive Refresh**: If access token expires in < 5 minutes, new tokens are generated
3. **Response Headers**: New tokens are added to the response headers:
   - `X-New-Access-Token`: New access token
   - `X-New-Refresh-Token`: New refresh token
   - `X-Token-Refreshed`: "true" (indicates rotation occurred)
4. **Client Updates**: Client reads these headers and updates stored tokens

### Benefits

- **Zero downtime**: Tokens refresh before expiration
- **Automatic**: No manual refresh endpoint calls needed
- **Transparent**: Works on any authenticated request
- **Secure**: Still uses token rotation (old tokens invalid)

### Client-Side Implementation with Auto-Rotation

### TypeScript/JavaScript Example

```typescript
class AuthService {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;

  async login(email: string, password: string) {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    const result = await response.json();

    if (result.responseType === 'Success') {
      // Store both tokens
      this.accessToken = result.data.token;
      this.refreshToken = result.data.refreshToken;

      localStorage.setItem('accessToken', this.accessToken);
      localStorage.setItem('refreshToken', this.refreshToken);

      return result.data.user;
    }

    throw new Error(result.responseMessage);
  }

  async makeAuthenticatedRequest(url: string, options: RequestInit = {}) {
    // Try with access token first
    let response = await fetch(url, {
      ...options,
      headers: {
        ...options.headers,
        'Authorization': `Bearer ${this.accessToken}`
      }
    });

    // If 401, try refreshing the token
    if (response.status === 401) {
      const refreshed = await this.refreshAccessToken();
      if (refreshed) {
        // Retry with new access token
        response = await fetch(url, {
          ...options,
          headers: {
            ...options.headers,
            'Authorization': `Bearer ${this.accessToken}`
          }
        });
      }
    }

    return response;
  }

  private async refreshAccessToken(): Promise<boolean> {
    try {
      const response = await fetch('/api/auth/refresh-token', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.refreshToken}`
        }
      });

      const result = await response.json();

      if (result.responseType === 'Success') {
        // Update both tokens (rotation!)
        this.accessToken = result.data.token;
        this.refreshToken = result.data.refreshToken;

        localStorage.setItem('accessToken', this.accessToken);
        localStorage.setItem('refreshToken', this.refreshToken);

        return true;
      }

      // Refresh failed - logout user
      this.logout();
      return false;
    } catch (error) {
      this.logout();
      return false;
    }
  }

  logout() {
    this.accessToken = null;
    this.refreshToken = null;
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login';
  }
}
```

### Handling Auto-Rotation in Client

```typescript
async makeAuthenticatedRequest(url: string, options: RequestInit = {}) {
  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${this.accessToken}`
    }
  });

  // Check for auto-rotated tokens in response headers
  const newAccessToken = response.headers.get('X-New-Access-Token');
  const newRefreshToken = response.headers.get('X-New-Refresh-Token');
  const tokenRefreshed = response.headers.get('X-Token-Refreshed');

  if (tokenRefreshed === 'true' && newAccessToken && newRefreshToken) {
    // Server auto-refreshed tokens - update storage
    this.accessToken = newAccessToken;
    this.refreshToken = newRefreshToken;
    localStorage.setItem('accessToken', newAccessToken);
    localStorage.setItem('refreshToken', newRefreshToken);
    console.log('Tokens auto-refreshed by server');
  }

  return response;
}
```

### Axios Interceptor Example with Auto-Rotation

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: '/api'
});

// Add access token to requests
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle auto-rotation and manual refresh on 401
api.interceptors.response.use(
  response => {
    // Check for auto-rotated tokens in response headers
    const newAccessToken = response.headers['x-new-access-token'];
    const newRefreshToken = response.headers['x-new-refresh-token'];
    const tokenRefreshed = response.headers['x-token-refreshed'];

    if (tokenRefreshed === 'true' && newAccessToken && newRefreshToken) {
      // Server auto-refreshed tokens - update storage
      localStorage.setItem('accessToken', newAccessToken);
      localStorage.setItem('refreshToken', newRefreshToken);
      console.log('Tokens auto-refreshed by server');
    }

    return response;
  },
  async error => {
    const originalRequest = error.config;

    // If 401 and haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const response = await axios.post('/api/auth/refresh-token', null, {
          headers: { Authorization: `Bearer ${refreshToken}` }
        });

        const { token, refreshToken: newRefreshToken } = response.data.data;

        // Update tokens
        localStorage.setItem('accessToken', token);
        localStorage.setItem('refreshToken', newRefreshToken);

        // Retry original request with new token
        originalRequest.headers.Authorization = `Bearer ${token}`;
        return axios(originalRequest);
      } catch (refreshError) {
        // Refresh failed - logout
        localStorage.clear();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
```

## Best Practices

### ✅ Do

1. **Store refresh token securely**
   - HttpOnly cookie (most secure)
   - localStorage (acceptable for SPAs)
   - Never in URL or query params

2. **Handle automatic token rotation**
   ```typescript
   // Check response headers on EVERY authenticated request
   if (response.headers.get('X-Token-Refreshed') === 'true') {
     const newAccessToken = response.headers.get('X-New-Access-Token');
     const newRefreshToken = response.headers.get('X-New-Refresh-Token');
     localStorage.setItem('accessToken', newAccessToken);
     localStorage.setItem('refreshToken', newRefreshToken);
   }
   ```

   **Note**: With automatic server-side rotation, you no longer need client-side timers. The server handles it when tokens are about to expire (< 5 minutes).

3. **Fallback to manual refresh on 401**
   ```typescript
   // If auto-rotation fails or token already expired
   if (response.status === 401) {
     await refreshAccessToken(); // Call /api/auth/refresh-token
     retryOriginalRequest();
   }
   ```

4. **Handle refresh failures gracefully**
   - Redirect to login
   - Clear all stored tokens
   - Show user-friendly message

5. **Use refresh token only for refresh endpoint**
   - Access token for API calls
   - Refresh token only for `/refresh-token` (fallback only)

### ❌ Don't

1. **Don't use access token for refresh**
   - Will fail - server checks `type` claim

2. **Don't reuse old refresh tokens**
   - They're invalidated after use
   - Always store the new one

3. **Don't store tokens in:**
   - URL parameters
   - Browser history
   - Console logs
   - Error messages

4. **Don't skip token rotation**
   - Always replace both tokens
   - Critical for security

## Security Features

### 1. Token Type Validation
```csharp
// Server validates token type
if (tokenType != "refresh")
    return Error("Invalid token type. Use refresh token.");
```

### 2. Short Access Token Lifetime
- Limits damage if stolen
- 15 minutes is security industry standard

### 3. Token Rotation
- Old refresh tokens become invalid
- Prevents replay attacks
- Detects token theft

### 4. Stateless (No Database)
- No DB lookups for validation
- Fast performance
- Scales horizontally

### 5. Standard JWT Claims
- `sub`: User ID
- `exp`: Expiration
- `iss`: Issuer
- `aud`: Audience

## Troubleshooting

### "Invalid token type" Error
**Problem**: Using access token for refresh endpoint

**Solution**: Use refresh token
```typescript
// Wrong
Authorization: Bearer <access-token>

// Correct
Authorization: Bearer <refresh-token>
```

### "Token expired" Error
**Problem**: Refresh token expired (> 7 days)

**Solution**: User must login again
```typescript
if (refreshTokenExpired) {
  logout();
  redirectToLogin();
}
```

### Tokens not rotating
**Problem**: Not storing new refresh token

**Solution**: Always update both tokens
```typescript
// After refresh
localStorage.setItem('accessToken', newToken);
localStorage.setItem('refreshToken', newRefreshToken); // Don't forget!
```

## Configuration

### appsettings.json
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "IPOClient",
    "Audience": "IPOClientUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Customizing Lifetimes

**More secure** (shorter access token):
```json
"AccessTokenExpirationMinutes": 5  // 5 minutes
```

**Less secure** (longer access token):
```json
"AccessTokenExpirationMinutes": 60  // 1 hour (not recommended)
```

**Longer refresh** (less frequent logins):
```json
"RefreshTokenExpirationDays": 30  // 30 days
```

## Testing

### Test Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin@123"}'
```

### Test Refresh
```bash
# Save refresh token from login response
REFRESH_TOKEN="eyJhbGc..."

curl -X POST http://localhost:5000/api/auth/refresh-token \
  -H "Authorization: Bearer $REFRESH_TOKEN"
```

### Verify Token Expiry
```typescript
function decodeToken(token: string) {
  const payload = JSON.parse(atob(token.split('.')[1]));
  const expiry = new Date(payload.exp * 1000);
  console.log('Expires:', expiry);
  console.log('Type:', payload.type);
}

decodeToken(accessToken);   // type: "access", exp: +15 min
decodeToken(refreshToken);  // type: "refresh", exp: +7 days
```

## Migration Guide

### Updating Existing Clients

**Before** (single token):
```typescript
localStorage.setItem('token', result.data.token);
```

**After** (dual tokens):
```typescript
localStorage.setItem('accessToken', result.data.token);
localStorage.setItem('refreshToken', result.data.refreshToken);
```

### Handling Old Tokens
Old tokens (24h) will continue working until they expire. New logins will receive both tokens.

## Summary

✅ **Access Token**: 15 minutes, for API calls
✅ **Refresh Token**: 7 days, for getting new access tokens
✅ **Token Rotation**: New tokens on every refresh
✅ **Stateless**: No database, pure JWT
✅ **Secure**: Industry-standard OAuth 2.0 pattern
✅ **Fast**: No DB lookups for validation

This implementation balances security and user experience, following OAuth 2.0 best practices.
