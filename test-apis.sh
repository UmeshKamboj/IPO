#!/bin/bash

# API Testing Script
# Usage: ./test-apis.sh

BASE_URL="https://financeapi.ivotiontech.co.in/api"
TOKEN=""

echo "================================"
echo "API Testing Script"
echo "================================"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to test an endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local data=$4

    echo -e "${YELLOW}Testing: ${description}${NC}"
    echo "Endpoint: ${method} ${endpoint}"

    if [ -z "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X ${method} \
            "${BASE_URL}${endpoint}" \
            -H "Authorization: Bearer ${TOKEN}" \
            -H "Content-Type: application/json")
    else
        response=$(curl -s -w "\n%{http_code}" -X ${method} \
            "${BASE_URL}${endpoint}" \
            -H "Authorization: Bearer ${TOKEN}" \
            -H "Content-Type: application/json" \
            -d "${data}")
    fi

    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✓ Success (${http_code})${NC}"
    else
        echo -e "${RED}✗ Failed (${http_code})${NC}"
    fi

    echo "Response: ${body:0:200}..."
    echo ""
}

# Test 1: Health Check (if you have one)
echo "=== 1. Health Check ==="
test_endpoint "GET" "/health" "API Health Check"

# Test 2: Get Groups
echo "=== 2. Get Groups ==="
test_endpoint "GET" "/ipos/groups?companyId=1&ipoId=11" "Get Groups by Company and IPO"

# Test 3: Get Order List (NO PAGINATION)
echo "=== 3. Get Order List (No Pagination) ==="
test_endpoint "GET" "/ipos/11/order/list" "Get All Orders for IPO 11"

# Test 4: Get Order List with Group Filter
echo "=== 4. Get Order List with Group Filter ==="
test_endpoint "GET" "/ipos/11/order/list?groupId=1" "Get Orders for IPO 11 and Group 1"

# Test 5: Get Top 5 Orders
echo "=== 5. Get Top 5 Orders ==="
test_endpoint "GET" "/ipos/11/orders/top5" "Get Top 5 Orders for IPO 11"

echo "================================"
echo "Testing Complete!"
echo "================================"
echo ""
echo "NOTE: Set TOKEN variable to test authenticated endpoints"
echo "Example: TOKEN='your-jwt-token' ./test-apis.sh"
