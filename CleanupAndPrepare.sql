-- =====================================================
-- Migration Cleanup Script
-- Purpose: Remove problematic migration history entries
-- Run this before creating new migrations
-- =====================================================

-- Step 1: Check current migration history
PRINT '=== Current Migration History ==='
SELECT [MigrationId], [ProductVersion]
FROM [__EFMigrationsHistory]
ORDER BY [MigrationId];
PRINT ''

-- Step 2: Delete problematic migration entries
PRINT '=== Deleting problematic migration entries ==='
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    '20260122072517_CheckChanges',
    '20260122072825_FixRatePrecision'
);

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' migration(s) deleted.'
PRINT ''

-- Step 3: Verify cleanup
PRINT '=== Migration History After Cleanup ==='
SELECT [MigrationId], [ProductVersion]
FROM [__EFMigrationsHistory]
ORDER BY [MigrationId];
PRINT ''

PRINT '=== Cleanup Complete ==='
PRINT 'Next steps:'
PRINT '1. Run: dotnet ef migrations add AddBuyerOrderTablesWithRatePrecision'
PRINT '2. Run: dotnet ef database update'
