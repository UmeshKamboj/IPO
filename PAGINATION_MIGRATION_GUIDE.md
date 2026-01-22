# Pagination and Global Search Migration Guide

## Overview
This document outlines the changes made to implement the new pagination format with global search support across all list APIs in the IPOClient application.

## New Pagination Format

### Request Format
```json
{
  "searchValue": "",  // Global search term (searches across multiple fields)
  "skip": 0,          // Number of records to skip (offset-based pagination)
  "pageSize": 25      // Number of records per page
}
```

### Response Format
```json
{
  "data": {
    "items": [...],           // Array of records
    "totalCount": 100,        // Total number of records matching filters
    "skip": 0,                // Current skip value
    "pageSize": 25,           // Records per page
    "currentPage": 1,         // Calculated: skip / pageSize + 1
    "totalPages": 4,          // Calculated: (totalCount + pageSize - 1) / pageSize
    "hasPreviousPage": false, // Calculated: skip > 0
    "hasNextPage": true,      // Calculated: skip + pageSize < totalCount
    "extras": {               // Optional extra metadata
      "totalApplications": 100,
      "pendingPanApplications": 15
    }
  },
  "responseType": 1,
  "responseMessage": "Success message",
  "responseCode": 200
}
```

## Changes Made

### 1. Created Base PaginationRequest DTO
**File:** `Models/Requests/PaginationRequest.cs`

```csharp
public class PaginationRequest
{
    public string SearchValue { get; set; } = string.Empty;
    public int Skip { get; set; } = 0;
    public int PageSize { get; set; } = 25;
}
```

### 2. Updated PagedResult Response Model
**File:** `Models/Responses/PagedResult.cs`

**Changes:**
- Replaced `PageNumber` with `Skip`
- Added calculated properties: `CurrentPage`, `TotalPages`, `HasPreviousPage`, `HasNextPage`
- Updated constructor to accept `skip` instead of `pageNumber`
- Added backward compatibility static method `FromPageNumber()` (marked as obsolete)

### 3. Updated Filter Request Models

All filter request models now inherit from `PaginationRequest`:

#### UserFilterRequest
**File:** `Models/Requests/UserFilterRequest.cs`
- Now inherits from `PaginationRequest`
- Global search searches across: Email, FirstName, LastName, Phone
- Specific filters can still be used alongside global search

#### IPOFilterRequest
**File:** `Models/Requests/IPOMaster/Request/IPOFilterRequest.cs`
- Now inherits from `PaginationRequest`
- Global search searches across: IPOName, Remark
- All specific IPO filters remain available

#### OrderDetailFilterRequest
**File:** `Models/Requests/IPOMaster/Request/OrderDetailFilterRequest.cs`
- Now inherits from `PaginationRequest`
- Global search searches across: PANNumber, ClientName, DematNumber, ApplicationNo
- All specific order filters remain available

### 4. Updated Repository Implementations

#### UserRepository
**File:** `Repositories/Implementations/UserRepository.cs`

**Method:** `GetUsersWithFiltersAsync()`
- Added `searchValue` parameter (searches Email, FName, LName, Phone)
- Changed from `pageNumber` to `skip` parameter
- Updated pagination logic: `.Skip(skip)` instead of `.Skip((pageNumber - 1) * pageSize)`

#### IPORepository
**File:** `Repositories/Implementations/IPORepository.cs`

**Method:** `GetIPOsWithFiltersAsync()`
- Added global search support (searches IPOName, Remark)
- Changed pagination from page-based to offset-based
- Updated constructor call to use `skip` instead of `pageNumber`

#### IPOBuyerPlaceOrderRepository
**File:** `Repositories/Implementations/IPOBuyerPlaceOrderRepository.cs`

**Method:** `GetOrderDetailPagedListAsync()`
- Added global search support (searches PANNumber, BankName, UPIID, DPId, BuyerName)
- Changed pagination from page-based to offset-based
- Maintains the `Extras` dictionary for additional metadata

### 5. Updated Service Layer

#### UserService
**File:** `Services/Implementations/UserService.cs`

**Method:** `GetAllUsersAsync()`
- Updated to pass `request.SearchValue` and `request.Skip` to repository
- Updated PagedResult constructor to use `skip` instead of `pageNumber`

## API Usage Examples

### Example 1: Simple Pagination (No Search)
```http
GET /api/users?skip=0&pageSize=25
```

### Example 2: Global Search with Pagination
```http
GET /api/users?searchValue=john&skip=0&pageSize=25
```
This will search for "john" across Email, FirstName, LastName, and Phone fields.

### Example 3: Global Search + Specific Filters
```http
GET /api/users?searchValue=john&email=@gmail.com&skip=0&pageSize=25
```
This combines global search with specific email filter.

### Example 4: IPO List with Global Search
```http
GET /api/ipos?searchValue=tech&skip=0&pageSize=25
```
Searches across IPOName and Remark fields.

### Example 5: Order Details with Global Search
```http
GET /api/ipos/123/orderdetail/buy?searchValue=ABCD1234&groupId=5&skip=0&pageSize=25&moduleName=buy
```
Searches across PANNumber, ClientName, DematNumber, and ApplicationNo.

## Migration Steps for Frontend

### Old Request Format
```javascript
const params = {
  pageNumber: 1,
  pageSize: 10,
  email: "admin@example.com"
};
```

### New Request Format
```javascript
const params = {
  searchValue: "",      // Add global search
  skip: 0,              // Change from pageNumber to skip
  pageSize: 25,         // Can keep or adjust default
  email: "admin@example.com"  // Specific filters still work
};
```

### Calculating Skip from Page Number
```javascript
// Old way
const pageNumber = 2;
const pageSize = 25;

// New way
const skip = (pageNumber - 1) * pageSize;  // skip = 25

const params = {
  searchValue: "",
  skip: skip,
  pageSize: pageSize
};
```

### Calculating Page Number from Skip
```javascript
const skip = 50;
const pageSize = 25;
const currentPage = Math.floor(skip / pageSize) + 1;  // currentPage = 3
```

## Response Changes

### Old Response Structure
```json
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 25,
  "totalPages": 4,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### New Response Structure
```json
{
  "items": [...],
  "totalCount": 100,
  "skip": 0,
  "pageSize": 25,
  "currentPage": 1,      // Calculated property
  "totalPages": 4,       // Calculated property
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## Benefits of New Approach

1. **Global Search**: Users can now search across multiple fields with a single search term
2. **Offset-Based Pagination**: More flexible for implementing "load more" patterns
3. **Backward Compatible**: Specific filters still work alongside global search
4. **Consistent Format**: All list APIs use the same pagination format
5. **Better UX**: Single search box can search across all relevant fields

## Testing Recommendations

1. Test pagination without search
2. Test global search functionality
3. Test combination of global search + specific filters
4. Test edge cases (skip > totalCount, pageSize = 0, etc.)
5. Test with existing frontend to ensure compatibility
6. Verify calculated properties (currentPage, totalPages, hasNextPage, etc.)

## Affected Endpoints

- `GET /api/users` - User list with pagination and global search
- `GET /api/ipos` - IPO list with pagination and global search
- `GET /api/ipos/{ipoId}/OrderDetail/BUY` - Order details with pagination and global search
- `GET /api/ipos/{ipoId}/Order` - Orders with pagination and global search

## Notes

- All changes are backward compatible at the repository level
- The `PagedResult.FromPageNumber()` method is marked as obsolete but still available for transition
- Global search is case-sensitive (can be changed to case-insensitive if needed using `EF.Functions.Like()`)
- Default `pageSize` changed from 10 to 25 (can be adjusted in `PaginationRequest`)
