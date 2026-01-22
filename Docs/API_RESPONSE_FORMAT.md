# API Response Format

## Overview
All API endpoints return a consistent response structure with human-readable status indicators.

## Response Structure

### Generic Response Format
```json
{
  "data": <T>,
  "responseType": "Success" | "Error" | "Warning",
  "responseMessage": "string",
  "responseCode": "string",
  "returnId": number | null
}
```

### Property Naming Convention
All JSON properties use **camelCase** (e.g., `responseType`, not `ResponseType`).

## Response Types

### 1. Success Response
Indicates successful operation.

**Example** (Login success):
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 1,
      "fName": "John",
      "lName": "Doe",
      "email": "john@example.com",
      "phone": "1234567890",
      "isAdmin": true,
      "createdDate": "2024-01-01T00:00:00Z",
      "expiryDate": null
    }
  },
  "responseType": "Success",
  "responseMessage": "Login successful",
  "responseCode": "200",
  "returnId": 1
}
```

### 2. Error Response
Indicates operation failed.

**Example** (Invalid credentials):
```json
{
  "data": null,
  "responseType": "Error",
  "responseMessage": "Invalid email or password",
  "responseCode": "401",
  "returnId": null
}
```

**Example** (Validation error):
```json
{
  "data": null,
  "responseType": "Error",
  "responseMessage": "Email and password are required",
  "responseCode": "400",
  "returnId": null
}
```

### 3. Warning Response
Indicates partial success or non-critical issues.

**Example**:
```json
{
  "data": {...},
  "responseType": "Warning",
  "responseMessage": "Operation completed with warnings",
  "responseCode": "206",
  "returnId": null
}
```

## Common Response Codes

### Success Codes
- `200` - OK (successful operation)
- `201` - Created (resource created successfully)

### Client Error Codes
- `400` - Bad Request (invalid input)
- `401` - Unauthorized (authentication required or failed)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found (resource doesn't exist)

### Server Error Codes
- `500` - Internal Server Error (unexpected server error)

## API Endpoints Examples

### Authentication

#### POST /api/auth/login
**Success Response:**
```json
{
  "data": {
    "token": "jwt-token-here",
    "user": { ... }
  },
  "responseType": "Success",
  "responseMessage": "Login successful",
  "responseCode": "200",
  "returnId": 1
}
```

**Error Response:**
```json
{
  "data": null,
  "responseType": "Error",
  "responseMessage": "Invalid email or password",
  "responseCode": "401",
  "returnId": null
}
```

#### POST /api/auth/refresh-token
**Success Response:**
```json
{
  "data": {
    "token": "new-jwt-token",
    "user": { ... }
  },
  "responseType": "Success",
  "responseMessage": "Token refreshed successfully",
  "responseCode": "200",
  "returnId": 1
}
```

### Users

#### GET /api/users
**Success Response:**
```json
{
  "data": {
    "items": [
      {
        "id": 1,
        "fName": "John",
        "lName": "Doe",
        "email": "john@example.com",
        ...
      }
    ],
    "totalCount": 50,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5
  },
  "responseType": "Success",
  "responseMessage": "Users retrieved successfully",
  "responseCode": "200",
  "returnId": null
}
```

#### POST /api/users/create
**Success Response:**
```json
{
  "data": {
    "id": 10,
    "fName": "Jane",
    "lName": "Smith",
    ...
  },
  "responseType": "Success",
  "responseMessage": "User created successfully",
  "responseCode": "200",
  "returnId": 10
}
```

**Error Response:**
```json
{
  "data": null,
  "responseType": "Error",
  "responseMessage": "Email already exists",
  "responseCode": "400",
  "returnId": null
}
```

### IPOs

#### GET /api/ipos
**Success Response:**
```json
{
  "data": {
    "items": [
      {
        "id": 1,
        "ipoName": "Company IPO",
        "ipoType": 1,
        ...
      }
    ],
    "totalCount": 25,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3
  },
  "responseType": "Success",
  "responseMessage": "IPOs retrieved successfully",
  "responseCode": "200",
  "returnId": null
}
```

#### POST /api/ipos/create
**Success Response:**
```json
{
  "data": {
    "id": 5,
    "ipoName": "New IPO",
    ...
  },
  "responseType": "Success",
  "responseMessage": "IPO created successfully",
  "responseCode": "200",
  "returnId": 5
}
```

## Client-Side Handling

### JavaScript/TypeScript Example

```typescript
interface ApiResponse<T> {
  data: T | null;
  responseType: 'Success' | 'Error' | 'Warning';
  responseMessage: string;
  responseCode: string;
  returnId: number | null;
}

async function login(email: string, password: string) {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

  const result: ApiResponse<LoginResponse> = await response.json();

  if (result.responseType === 'Success') {
    // Handle success
    console.log('Login successful:', result.data);
    localStorage.setItem('token', result.data.token);
  } else if (result.responseType === 'Error') {
    // Handle error
    console.error('Login failed:', result.responseMessage);
    alert(result.responseMessage);
  }
}
```

### Checking Response Type

```typescript
// Type-safe checking
if (response.responseType === 'Success') {
  // Success logic
}

// Alternative: use the success flag
if (response.success) {  // Derived from ResponseType
  // Success logic
}
```

### Error Handling Pattern

```typescript
async function apiCall<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, options);
  const result: ApiResponse<T> = await response.json();

  if (result.responseType === 'Error') {
    throw new Error(result.responseMessage);
  }

  if (result.responseType === 'Warning') {
    console.warn(result.responseMessage);
  }

  return result.data!;
}

// Usage
try {
  const users = await apiCall<User[]>('/api/users');
  console.log('Users:', users);
} catch (error) {
  console.error('API Error:', error.message);
}
```

## Benefits of String Enums

### Before (Numeric)
```json
{
  "responseType": 1,  // What does 1 mean?
  ...
}
```

### After (String)
```json
{
  "responseType": "Success",  // Clear and readable
  ...
}
```

### Advantages
1. **Self-documenting**: No need to look up enum values
2. **Debugging**: Easier to read in logs and network traces
3. **Version-safe**: Adding new types doesn't break existing clients
4. **Type-safe**: TypeScript unions work perfectly
5. **Human-readable**: Non-technical stakeholders can understand responses

## Migration Notes

### Breaking Change
If you had existing clients expecting numeric response types (1, 2, 3), they will now receive strings ("Success", "Error", "Warning").

### Update Client Code

**Before:**
```typescript
if (response.responseType === 1) { ... }
```

**After:**
```typescript
if (response.responseType === 'Success') { ... }
```

### Backward Compatibility
If you need numeric values temporarily:

```csharp
// In ResponseType.cs
public enum ResponseType
{
    [JsonPropertyName("Success")]
    Success = 1,

    [JsonPropertyName("Error")]
    Error = 2,

    [JsonPropertyName("Warning")]
    Warning = 3
}
```

But this is not recommended. String enums are the modern standard.

## Testing

### Example API Tests

```bash
# Login success
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin@123"}'

# Expected response:
{
  "data": {...},
  "responseType": "Success",  # <- String, not number
  "responseMessage": "Login successful",
  "responseCode": "200",
  "returnId": 1
}

# Login error
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"wrong@test.com","password":"wrong"}'

# Expected response:
{
  "data": null,
  "responseType": "Error",  # <- String, not number
  "responseMessage": "Invalid email or password",
  "responseCode": "401",
  "returnId": null
}
```

## Standard HTTP Status Codes

The API also sets appropriate HTTP status codes:

| ResponseType | ResponseCode | HTTP Status |
|--------------|--------------|-------------|
| Success      | 200          | 200 OK      |
| Success      | 201          | 201 Created |
| Error        | 400          | 400 Bad Request |
| Error        | 401          | 401 Unauthorized |
| Error        | 403          | 403 Forbidden |
| Error        | 404          | 404 Not Found |
| Error        | 500          | 500 Internal Server Error |
| Warning      | 206          | 206 Partial Content |

## Recommendations

1. **Always check responseType** - Don't rely solely on HTTP status codes
2. **Display responseMessage to users** - It contains human-readable error messages
3. **Log responseCode** - Useful for debugging and error tracking
4. **Use returnId for created resources** - Automatically set when creating entities
5. **Handle all three types** - Success, Error, and Warning

## Complete Example

```typescript
interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  token: string;
  user: {
    id: number;
    fName: string;
    lName: string;
    email: string;
    isAdmin: boolean;
  };
}

async function login(credentials: LoginRequest) {
  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    });

    const result: ApiResponse<LoginResponse> = await response.json();

    switch (result.responseType) {
      case 'Success':
        localStorage.setItem('token', result.data!.token);
        return { success: true, data: result.data };

      case 'Error':
        return { success: false, error: result.responseMessage };

      case 'Warning':
        console.warn('Login warning:', result.responseMessage);
        return { success: true, data: result.data, warning: result.responseMessage };

      default:
        return { success: false, error: 'Unknown response type' };
    }
  } catch (error) {
    return { success: false, error: 'Network error' };
  }
}
```
