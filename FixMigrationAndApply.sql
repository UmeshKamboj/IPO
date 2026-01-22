-- =====================================================
-- Fix Migration State and Create Missing Tables
-- =====================================================

-- Step 1: Remove the migration from history so we can reapply it
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = '20260122074119_AddBuyerOrderTablesWithRatePrecision';

PRINT 'Migration removed from history. Now run: dotnet ef database update'
PRINT ''
PRINT 'This will create the following tables:'
PRINT '  - IPO_GroupMaster'
PRINT '  - IPO_BuyerPlaceOrderMaster'
PRINT '  - IPO_BuyerOrder (with Rate as decimal(18,4))'
PRINT '  - IPO_PlaceOrderChild'
