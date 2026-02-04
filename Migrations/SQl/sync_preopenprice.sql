-- Sync PreOpenPrice from parent IPO to all children with PreOpenPrice = 0
UPDATE c
SET c.PreOpenPrice = ISNULL(m.OpenIPOPrice, 0),
    c.ModifiedDate = GETUTCDATE(),
    c.ModifiedBy = 'Migration'
FROM IPO_PlaceOrderChild c
INNER JOIN IPO_BuyerOrder o ON c.OrderId = o.OrderId
INNER JOIN IPO_BuyerPlaceOrderMaster bm ON o.BuyerMasterId = bm.BuyerMasterId
INNER JOIN IPO_IPOMaster m ON bm.IPOId = m.Id
WHERE c.PreOpenPrice = 0
  AND c.IsDeleted = 0
  AND o.IsDeleted = 0
  AND bm.IsDeleted = 0
  AND bm.IsActive = 1;

SELECT @@ROWCOUNT AS UpdatedRecords;
