-- Clean up migration history to allow fresh migrations
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    '20260122072517_CheckChanges',
    '20260122072825_FixRatePrecision'
);

-- Verify remaining migrations
SELECT * FROM [__EFMigrationsHistory] ORDER BY [MigrationId];
