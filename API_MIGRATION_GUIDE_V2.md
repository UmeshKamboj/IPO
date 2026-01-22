# API Migration Guide - V2 (POST with Global Search Only)

## Overview
All list APIs have been updated to use POST method with global search only. Specific filters have been removed to simplify the API.

## Changes Summary

### 1. **Method Changed**: GET → POST
All list endpoints now use POST method with request body instead of query parameters.

### 2. **Global Search Only**
- All specific filter fields removed (email, firstName, IPOName, etc.)
- Only `searchValue` is available for searching
- Each endpoint searches across multiple relevant fields

### 3. **Default Values**
- `skip`: 0 (default)
- `pageSize`: 10 (default, changed from 25)
- `searchValue`: "" (empty string, searches all records)

## New Request Format

### Standard Pagination Request
```json
{
  "searchValue": "",
  "skip": 0,
  "pageSize": 10
}
```

### Order Request (with required filters)
```json
{
  "searchValue": "",
  "skip": 0,
  "pageSize": 10,
  "moduleName": "buy",
  "groupId": null,
  "orderCategoryId": null,
  "investorTypeId": null
}
```

## API Endpoints

### 1. Users List
**Endpoint:** `POST /api/users/list`

**Request Body:**
```json
{
  "searchValue": "john",
  "skip": 0,
  "pageSize": 10
}
```

**Global Search Fields:**
- Email
- FirstName (FName)
- LastName (LName)
- Phone

**Example:**
```bash
curl -X POST https://your-api.com/api/users/list \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "searchValue": "john",
    "skip": 0,
    "pageSize": 10
  }'
```

---

### 2. IPO List
**Endpoint:** `POST /api/ipos/list`

**Request Body:**
```json
{
  "searchValue": "tech",
  "skip": 0,
  "pageSize": 10
}
```

**Global Search Fields:**
- IPOName
- Remark

**Example:**
```bash
curl -X POST https://your-api.com/api/ipos/list \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "searchValue": "tech",
    "skip": 0,
    "pageSize": 10
  }'
```

---

### 3. Order List
**Endpoint:** `POST /api/ipos/{ipoId}/orderdetail/list`
**Alternative:** `POST /api/ipos/{ipoId}/order/list`

**Request Body:**
```json
{
  "searchValue": "ABCD1234",
  "skip": 0,
  "pageSize": 10,
  "moduleName": "buy",
  "groupId": 5,
  "orderCategoryId": 1,
  "investorTypeId": 2
}
```

**Required Field:**
- `moduleName` (string): "buy", "sell", or "all"

**Optional Filters:**
- `groupId` (int?): Filter by group
- `orderCategoryId` (int?): Filter by order category
- `investorTypeId` (int?): Filter by investor type

**Global Search Fields:**
- PANNumber
- ClientName
- DematNumber
- ApplicationNo

**Example:**
```bash
curl -X POST https://your-api.com/api/ipos/123/orderdetail/list \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "searchValue": "ABCD1234",
    "skip": 0,
    "pageSize": 10,
    "moduleName": "buy",
    "groupId": 5
  }'
```

---

## Response Format

All endpoints return the same response structure:

```json
{
  "data": {
    "items": [
      { /* record 1 */ },
      { /* record 2 */ }
    ],
    "totalCount": 100,
    "skip": 0,
    "pageSize": 10,
    "currentPage": 1,
    "totalPages": 10,
    "hasPreviousPage": false,
    "hasNextPage": true,
    "extras": {
      "totalApplications": 100,
      "pendingPanApplications": 15
    }
  },
  "responseType": 1,
  "responseMessage": "Success message",
  "responseCode": 200,
  "returnId": null
}
```

## Migration from V1

### Old API (V1)
```http
GET /api/users?pageNumber=1&pageSize=10&email=john@example.com&firstName=John
```

### New API (V2)
```http
POST /api/users/list
Content-Type: application/json

{
  "searchValue": "john",
  "skip": 0,
  "pageSize": 10
}
```

### Key Differences

| Feature | V1 (Old) | V2 (New) |
|---------|----------|----------|
| HTTP Method | GET | POST |
| Parameters | Query string | Request body (JSON) |
| Pagination | `pageNumber` (1-based) | `skip` (offset-based) |
| Search | Multiple specific filters | Single `searchValue` |
| Default pageSize | 10 (users), varies | 10 (all endpoints) |

## Frontend Implementation

### JavaScript/TypeScript Example

```typescript
// API service
async function fetchUsers(searchValue: string = "", skip: number = 0, pageSize: number = 10) {
  const response = await fetch('/api/users/list', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      searchValue,
      skip,
      pageSize
    })
  });

  return await response.json();
}

// Usage
const result = await fetchUsers("john", 0, 10);
console.log(result.data.items); // Array of users
console.log(result.data.totalCount); // Total matching records
```

### Pagination Helper

```typescript
interface PaginationState {
  searchValue: string;
  skip: number;
  pageSize: number;
  currentPage: number;
}

class PaginationHelper {
  state: PaginationState = {
    searchValue: "",
    skip: 0,
    pageSize: 10,
    currentPage: 1
  };

  setPage(pageNumber: number) {
    this.state.skip = (pageNumber - 1) * this.state.pageSize;
    this.state.currentPage = pageNumber;
  }

  setSearch(searchValue: string) {
    this.state.searchValue = searchValue;
    this.state.skip = 0; // Reset to first page
    this.state.currentPage = 1;
  }

  nextPage() {
    this.state.skip += this.state.pageSize;
    this.state.currentPage++;
  }

  previousPage() {
    if (this.state.skip >= this.state.pageSize) {
      this.state.skip -= this.state.pageSize;
      this.state.currentPage--;
    }
  }

  getRequestBody() {
    return {
      searchValue: this.state.searchValue,
      skip: this.state.skip,
      pageSize: this.state.pageSize
    };
  }
}
```

## Testing Examples

### Test 1: Get First Page (No Search)
```bash
POST /api/users/list
{
  "searchValue": "",
  "skip": 0,
  "pageSize": 10
}
# Expected: First 10 users
```

### Test 2: Global Search
```bash
POST /api/users/list
{
  "searchValue": "john",
  "skip": 0,
  "pageSize": 10
}
# Expected: Users matching "john" in email, firstName, lastName, or phone
```

### Test 3: Second Page
```bash
POST /api/users/list
{
  "searchValue": "",
  "skip": 10,
  "pageSize": 10
}
# Expected: Records 11-20
```

### Test 4: Large Page Size
```bash
POST /api/users/list
{
  "searchValue": "",
  "skip": 0,
  "pageSize": 100
}
# Expected: First 100 users
```

### Test 5: Search with Pagination
```bash
POST /api/users/list
{
  "searchValue": "admin",
  "skip": 20,
  "pageSize": 10
}
# Expected: Records 21-30 that match "admin"
```

## Common Patterns

### Pattern 1: Load More / Infinite Scroll
```typescript
let skip = 0;
const pageSize = 20;
let allItems = [];

async function loadMore() {
  const response = await fetchUsers("", skip, pageSize);
  allItems = [...allItems, ...response.data.items];
  skip += pageSize;

  return response.data.hasNextPage;
}
```

### Pattern 2: Search with Debounce
```typescript
let searchTimeout;

function onSearchChange(searchValue: string) {
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(async () => {
    const result = await fetchUsers(searchValue, 0, 10);
    // Update UI with result
  }, 300); // Wait 300ms after user stops typing
}
```

### Pattern 3: Table Pagination
```typescript
interface TableState {
  currentPage: number;
  pageSize: number;
  searchValue: string;
  totalPages: number;
}

async function loadPage(page: number, state: TableState) {
  const skip = (page - 1) * state.pageSize;
  const result = await fetchUsers(state.searchValue, skip, state.pageSize);

  return {
    items: result.data.items,
    totalPages: result.data.totalPages,
    currentPage: page
  };
}
```

## Error Handling

### Example Error Response
```json
{
  "data": null,
  "responseType": 2,
  "responseMessage": "ModuleName is required",
  "responseCode": 400,
  "returnId": null
}
```

### Client-Side Error Handling
```typescript
try {
  const response = await fetch('/api/users/list', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(requestBody)
  });

  const result = await response.json();

  if (result.responseType !== 1) {
    // Handle error
    console.error(result.responseMessage);
    return null;
  }

  return result.data;
} catch (error) {
  console.error('Network error:', error);
  return null;
}
```

## Best Practices

1. **Always send all three fields** in the request body (even if using defaults)
2. **Reset skip to 0** when changing search value
3. **Implement debouncing** for search input (300-500ms recommended)
4. **Show loading indicators** during API calls
5. **Handle empty results** gracefully
6. **Cache results** when appropriate to reduce API calls
7. **Use hasNextPage** to determine if more data is available
8. **Validate moduleName** for order endpoints before sending

## Breaking Changes

⚠️ **Important:** The following V1 features are NO LONGER SUPPORTED:

- ❌ GET method for list endpoints
- ❌ Query string parameters (`?pageNumber=1&pageSize=10`)
- ❌ Specific filter fields (email, firstName, lastName, IPOName, IPOType, etc.)
- ❌ `pageNumber` parameter (use `skip` instead)
- ❌ Date range filters (expiryDateFrom, expiryDateTo)
- ❌ Numeric filters (IPO_Upper_Price_Band, Total_IPO_Size_Cr, etc.)

## Support

For questions or issues with the migration, please refer to:
- `PAGINATION_MIGRATION_GUIDE.md` for detailed technical changes
- API documentation at `/swagger` endpoint
- Contact the development team

---

**Version:** 2.0
**Last Updated:** 2026-01-21
**Compatibility:** Breaking changes from V1
