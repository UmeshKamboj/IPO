# API Testing Guide

## Quick Test Summary

### 🔧 Prerequisites
1. ✅ Application is running
2. ✅ Database migrations applied
3. ✅ Have a valid JWT token (from login)

---

## 🚀 Testing Methods

### Option 1: PowerShell Script (Windows)
```powershell
# Basic test (no auth required endpoints)
.\test-apis.ps1

# With authentication
.\test-apis.ps1 -Token "your-jwt-token-here"

# Custom settings
.\test-apis.ps1 -Token "your-token" -CompanyId 1 -IpoId 11
```

### Option 2: Manual Browser/Postman Testing

#### 1️⃣ Test Groups API
```
GET https://financeapi.ivotiontech.co.in/api/ipos/groups?companyId=1&ipoId=11
Headers:
  Authorization: Bearer {your-token}
```

**Expected:**
- Status: 200 OK
- Returns list of groups with `IPOId`, `GroupName`, `CreatedBy`, `ModifiedBy`, `ModifiedDate`

---

#### 2️⃣ Test Order List (NO PAGINATION) ✨
```
GET https://financeapi.ivotiontech.co.in/api/ipos/11/order/list
Headers:
  Authorization: Bearer {your-token}
```

**Expected:**
- Status: 200 OK
- Returns ALL orders (no pagination)
- Each order has: `OrderId`, `GroupName`, `OrderTypeName`, `Rate`, etc.

---

#### 3️⃣ Test Order List with Group Filter ✨
```
GET https://financeapi.ivotiontech.co.in/api/ipos/11/order/list?groupId=1
Headers:
  Authorization: Bearer {your-token}
```

**Expected:**
- Status: 200 OK
- Returns only orders for `groupId=1`
- No pagination

---

#### 4️⃣ Test Top 5 Orders
```
GET https://financeapi.ivotiontech.co.in/api/ipos/11/orders/top5
Headers:
  Authorization: Bearer {your-token}
```

**Expected:**
- Status: 200 OK
- Returns maximum 5 recent orders
- Ordered by newest first

---

#### 5️⃣ Test Order Details (NO PAGINATION) ✨
```
GET https://financeapi.ivotiontech.co.in/api/ipos/11/order-details?orderType=1
Headers:
  Authorization: Bearer {your-token}
```

**Expected:**
- Status: 200 OK
- Returns ALL order details (child records)
- Includes: `PANNumber`, `ClientName`, `DematNumber`, `ApplicationNo`, etc.
- No pagination

---

#### 6️⃣ Test Create Order
```
POST https://financeapi.ivotiontech.co.in/api/ipos/buyer-orders
Headers:
  Authorization: Bearer {your-token}
  Content-Type: application/json

Body:
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
      "rate": 100.5000
    }
  ]
}
```

**Expected:**
- Status: 201 Created
- Returns created order with `BuyerMasterId`
- Rate stored with 4 decimal precision

---

## ✅ Verification Checklist

### Database Migrations
- [ ] `IPO_GroupMaster` table has columns: `CreatedBy`, `IPOId`, `ModifiedBy`, `ModifiedDate`
- [ ] `IPO_BuyerOrder` table exists with `Rate` as `decimal(18,4)`
- [ ] `IPO_PlaceOrderChild` table exists (renamed from `ChildPlaceOrder`)
- [ ] Foreign key constraint from `IPO_BuyerPlaceOrderMaster.GroupId` to `IPO_GroupMaster.IPOGroupId`
- [ ] Migrations applied: `20260122072854_UpdateRatePrecision`, `20260122074119_AddBuyerOrderTablesWithRatePrecision`, and `20260122102505_AddGroupForeignKeyToBuyerMaster`

### API Functionality
- [ ] Groups endpoint returns all groups (with optional ipoId filter)
- [ ] Order list returns ALL orders without pagination
- [ ] Order list can filter by groupId
- [ ] Order details returns ALL child records without pagination
- [ ] Rate precision is 4 decimal places
- [ ] Search functionality works (if SearchValue is uncommented)

### Code Changes
- [ ] `GetOrderPagedListAsync` returns `List` instead of `PagedResult`
- [ ] `GetOrderDetailPagedListAsync` returns `List` instead of `PagedResult`
- [ ] `OrderDetailFilterRequest` only has `GroupId` (other props commented)
- [ ] `IPO_PlaceOrderChild` table configuration added to DbContext
- [ ] Automatic migration on startup enabled

---

## 🐛 Common Issues

### Issue: "Invalid object name 'ChildPlaceOrder'"
**Solution:** Fixed - Table renamed to `IPO_PlaceOrderChild` in migration `20260122102505_AddGroupForeignKeyToBuyerMaster`

### Issue: "Invalid column name 'CreatedBy'" (on Groups)
**Solution:** Run `AddMissingColumnsNow.sql` or wait for automatic migration on startup

### Issue: Rate precision warnings
**Solution:** Already fixed - `Rate` configured as `decimal(18,4)`

### Issue: Group filter not working
**Solution:** Already fixed - Query logic updated to use `(!ipoId.HasValue || x.IPOId == ipoId.Value)`

### Issue: "An error occurred while saving the entity changes" (on Create Buyer Order)
**Solution:** Fixed - Added foreign key relationship from `IPO_BuyerPlaceOrderMaster.GroupId` to `IPO_GroupMaster.IPOGroupId` in DbContext configuration

---

## 📊 Expected Results Summary

| Endpoint | Method | Pagination | Filters | Status |
|----------|--------|------------|---------|--------|
| /ipos/groups | GET | ❌ No | companyId, ipoId | ✅ Fixed |
| /ipos/{id}/order/list | GET | ❌ No | groupId | ✅ Updated |
| /ipos/{id}/orders/top5 | GET | Limit 5 | - | ✅ Working |
| /ipos/{id}/order-details | GET | ❌ No | orderType, groupId | ✅ Updated |
| /ipos/buyer-orders | POST | - | - | ✅ Working |
| /ipos/order-details | PUT | - | - | ✅ Working |

---

## 🎯 Success Criteria

All tests should:
1. ✅ Return HTTP 200 (or 201 for POST)
2. ✅ Return valid JSON response
3. ✅ Include proper data structure
4. ✅ NOT include pagination metadata (skip, pageSize, totalCount)
5. ✅ Handle filters correctly
6. ✅ Store Rate with 4 decimal precision

---

## 📝 Notes

- **No Pagination:** Order list and order details now return ALL records
- **Filters:** GroupId filter available on order endpoints
- **Search:** SearchValue property exists but is commented in request model
- **Precision:** Rate values stored as decimal(18,4)
- **Auto Migration:** Enabled in Program.cs - applies pending migrations on startup
