-- ============================================
-- Client Wise Billing Amount Calculation Update
-- ============================================
--
-- OLD FORMULA: Amount = AllotedQty * PreOpenPrice
-- NEW FORMULA: Amount = (PreOpenPrice - IPOPrice) * AllotedQty - Rate
--
-- Where:
--   - PreOpenPrice = IPO_PlaceOrderChild.PreOpenPrice (or IPO_IPOMaster.OpenIPOPrice if child's is 0)
--   - IPOPrice = IPO_IPOMaster.IPO_Upper_Price_Band
--   - AllotedQty = IPO_PlaceOrderChild.AllotedQty
--   - Rate = IPO_BuyerOrder.Rate
--
-- NOTE: Amount is NOT stored in the database. It is calculated on-the-fly in the application.
--       This SQL file is for verification and reporting purposes only.
-- ============================================

-- ============================================
-- QUERY: View Client Wise Billing with New Amount Calculation
-- ============================================
SELECT
    c.POChildId,
    c.PANNumber,
    c.ClientName,
    c.AllotedQty,
    o.Rate,
    CASE
        WHEN c.PreOpenPrice > 0 THEN c.PreOpenPrice
        ELSE ISNULL(m.OpenIPOPrice, 0)
    END AS PreOpenPrice,
    m.IPO_Upper_Price_Band AS IPOPrice,
    -- NEW FORMULA: (PreOpenPrice - IPOPrice) * AllotedQty - Rate
    (
        (CASE WHEN c.PreOpenPrice > 0 THEN c.PreOpenPrice ELSE ISNULL(m.OpenIPOPrice, 0) END)
        - ISNULL(m.IPO_Upper_Price_Band, 0)
    ) * ISNULL(c.AllotedQty, 0) - o.Rate AS Amount,
    g.GroupName,
    m.IPOName
FROM IPO_PlaceOrderChild c
INNER JOIN IPO_BuyerOrder o ON c.OrderId = o.OrderId
INNER JOIN IPO_BuyerPlaceOrderMaster bm ON o.BuyerMasterId = bm.BuyerMasterId
INNER JOIN IPO_IPOMaster m ON bm.IPOId = m.Id
LEFT JOIN IPO_GroupMaster g ON c.GroupId = g.IPOGroupId
WHERE c.IsDeleted = 0
  AND o.IsDeleted = 0
  AND bm.IsDeleted = 0
  AND bm.IsActive = 1
ORDER BY c.POChildId;

-- ============================================
-- QUERY: Get Total Billing Amount per IPO with New Formula
-- ============================================
SELECT
    m.Id AS IPOId,
    m.IPOName,
    m.IPO_Upper_Price_Band AS IPOPrice,
    ISNULL(m.OpenIPOPrice, 0) AS PreOpenPrice,
    SUM(
        (
            (CASE WHEN c.PreOpenPrice > 0 THEN c.PreOpenPrice ELSE ISNULL(m.OpenIPOPrice, 0) END)
            - ISNULL(m.IPO_Upper_Price_Band, 0)
        ) * ISNULL(c.AllotedQty, 0) - o.Rate
    ) AS TotalAmount
FROM IPO_PlaceOrderChild c
INNER JOIN IPO_BuyerOrder o ON c.OrderId = o.OrderId
INNER JOIN IPO_BuyerPlaceOrderMaster bm ON o.BuyerMasterId = bm.BuyerMasterId
INNER JOIN IPO_IPOMaster m ON bm.IPOId = m.Id
WHERE c.IsDeleted = 0
  AND o.IsDeleted = 0
  AND bm.IsDeleted = 0
  AND bm.IsActive = 1
GROUP BY m.Id, m.IPOName, m.IPO_Upper_Price_Band, m.OpenIPOPrice
ORDER BY m.Id;

-- ============================================
-- QUERY: Verify Formula with Sample Data
-- Example: PreOpenPrice=72, IPOPrice=59, AllotedQty=10, Rate=25
-- Expected Amount = (72 - 59) * 10 - 25 = 130 - 25 = 105
-- ============================================
SELECT
    72 AS PreOpenPrice,
    59 AS IPOPrice,
    10 AS AllotedQty,
    25 AS Rate,
    (72 - 59) * 10 - 25 AS ExpectedAmount;

-- Another example: PreOpenPrice=72, IPOPrice=59, AllotedQty=1, Rate=25
-- Expected Amount = (72 - 59) * 1 - 25 = 13 - 25 = -12
SELECT
    72 AS PreOpenPrice,
    59 AS IPOPrice,
    1 AS AllotedQty,
    25 AS Rate,
    (72 - 59) * 1 - 25 AS ExpectedAmount;
