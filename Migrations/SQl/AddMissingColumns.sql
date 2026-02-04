-- =====================================================
-- Add Missing Columns to IPO_GroupMaster and IPO_BuyerPlaceOrderMaster
-- =====================================================

-- Add missing columns to IPO_GroupMaster
ALTER TABLE [IPO_GroupMaster]
ADD [CreatedBy] int NULL;

ALTER TABLE [IPO_GroupMaster]
ADD [IPOId] int NOT NULL DEFAULT 0;

ALTER TABLE [IPO_GroupMaster]
ADD [ModifiedBy] int NULL;

ALTER TABLE [IPO_GroupMaster]
ADD [ModifiedDate] datetime2 NULL;

PRINT 'Missing columns added to IPO_GroupMaster';

-- Note: IPO_BuyerPlaceOrderMaster already has CreatedBy, ModifiedBy, ModifiedDate
-- Verify the structure
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('IPO_GroupMaster', 'IPO_BuyerPlaceOrderMaster')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
