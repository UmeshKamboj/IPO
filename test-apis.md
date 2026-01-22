# API Testing Checklist

Base URL: `https://financeapi.ivotiontech.co.in/api`

## 1. Authentication APIs

### Login
```bash
POST /auth/login
Content-Type: application/json

{
  "email": "your-email@example.com",
  "password": "your-password"
}
```

## 2. IPO APIs

### Get All IPOs
```bash
GET /ipos?companyId=1
Authorization: Bearer {token}
```

### Get IPO by ID
```bash
GET /ipos/{id}?companyId=1
Authorization: Bearer {token}
```

### Create IPO
```bash
POST /ipos
Authorization: Bearer {token}
Content-Type: application/json

{
  "ipoName": "Test IPO",
  "ipoType": 1,
  "ipo_Upper_Price_Band": 100.50,
  "total_IPO_Size_Cr": 500.00,
  "ipo_Retail_Lot_Size": 10,
  ...
}
```

## 3. Group APIs

### Get Groups by Company
```bash
GET /ipos/groups?companyId=1&ipoId=11
Authorization: Bearer {token}
```

### Create Group
```bash
POST /ipos/groups
Authorization: Bearer {token}
Content-Type: application/json

{
  "groupName": "Test Group",
  "ipoId": 11
}
```

## 4. Buyer Order APIs (Recently Updated - NO PAGINATION)

### Get Order List (No Pagination)
```bash
GET /ipos/11/order/list?groupId=1
Authorization: Bearer {token}
```

### Get Top 5 Orders
```bash
GET /ipos/11/orders/top5
Authorization: Bearer {token}
```

### Create Buyer Order
```bash
POST /ipos/buyer-orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "ipoId": 11,
  "groupId": 1,
  "dateTime": "2026-01-22T10:00:00Z",
  "orders": [
    {
      "orderType": 1,
      "orderCategory": 1,
      "investorType": 1,
      "quantity": 5,
      "rate": 100.50
    }
  ]
}
```

### Get Order Details (No Pagination)
```bash
GET /ipos/11/order-details?orderType=1&groupId=1
Authorization: Bearer {token}
```

### Update Order Details
```bash
PUT /ipos/order-details
Authorization: Bearer {token}
Content-Type: application/json

{
  "orders": [
    {
      "poChildId": 1,
      "panNumber": "ABCDE1234F",
      "clientName": "John Doe",
      "allotedQty": 10,
      "dematNumber": "12345678",
      "applicationNo": "APP123"
    }
  ]
}
```

## 5. User APIs

### Get Users
```bash
GET /users?companyId=1
Authorization: Bearer {token}
```

### Create User
```bash
POST /users
Authorization: Bearer {token}
Content-Type: application/json

{
  "fName": "John",
  "lName": "Doe",
  "email": "john.doe@example.com",
  "password": "securePassword123",
  "phone": "1234567890",
  "isAdmin": false
}
```

---

## Test Status

- [ ] Authentication - Login
- [ ] IPOs - Get All
- [ ] IPOs - Get by ID
- [ ] IPOs - Create
- [ ] Groups - Get by Company
- [ ] Groups - Create
- [ ] Orders - Get List (No Pagination) ✨ NEW
- [ ] Orders - Get Top 5
- [ ] Orders - Create
- [ ] Order Details - Get (No Pagination) ✨ NEW
- [ ] Order Details - Update
- [ ] Users - Get All
- [ ] Users - Create

---

## Recent Changes Summary

### ✅ Removed Pagination
- **GET /ipos/{ipoId}/order/list** - Now returns ALL orders (no skip/take)
- **GET /ipos/{ipoId}/order-details** - Now returns ALL order details (no skip/take)

### ✅ Filters Available
- `groupId` - Filter by specific group
- `searchValue` - Search across PAN, Client Name, Demat Number, Application Number (commented out in request model but available in code)

### ✅ Database Migration
- Automatic migration on startup enabled
- Rate precision fixed (decimal(18,4))
- All tables created and configured

### ✅ Query Logic Fixed
- IPO_GroupMaster - Fixed filter logic for optional ipoId parameter
