-- =====================================================
-- Add Missing Columns to Existing Tables
-- =====================================================

BEGIN TRANSACTION;

-- Add missing columns to IPO_GroupMaster if they don't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IPO_GroupMaster' AND COLUMN_NAME = 'CreatedBy')
BEGIN
    ALTER TABLE [IPO_GroupMaster] ADD [CreatedBy] int NULL;
    PRINT 'Added CreatedBy to IPO_GroupMaster';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IPO_GroupMaster' AND COLUMN_NAME = 'IPOId')
BEGIN
    ALTER TABLE [IPO_GroupMaster] ADD [IPOId] int NOT NULL DEFAULT 0;
    PRINT 'Added IPOId to IPO_GroupMaster';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IPO_GroupMaster' AND COLUMN_NAME = 'ModifiedBy')
BEGIN
    ALTER TABLE [IPO_GroupMaster] ADD [ModifiedBy] int NULL;
    PRINT 'Added ModifiedBy to IPO_GroupMaster';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IPO_GroupMaster' AND COLUMN_NAME = 'ModifiedDate')
BEGIN
    ALTER TABLE [IPO_GroupMaster] ADD [ModifiedDate] datetime2 NULL;
    PRINT 'Added ModifiedDate to IPO_GroupMaster';
END

-- Add foreign key constraint for IPOId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_IPO_GroupMaster_IPO_IPOMaster_IPOId')
BEGIN
    -- First, check if IPO_IPOMaster table exists
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IPO_IPOMaster')
    BEGIN
        ALTER TABLE [IPO_GroupMaster]
        ADD CONSTRAINT FK_IPO_GroupMaster_IPO_IPOMaster_IPOId
        FOREIGN KEY (IPOId) REFERENCES [IPO_IPOMaster](Id);
        PRINT 'Added foreign key constraint for IPOId';
    END
END

-- Add index for IPOId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_IPO_GroupMaster_IPOId')
BEGIN
    CREATE INDEX IX_IPO_GroupMaster_IPOId ON [IPO_GroupMaster](IPOId);
    PRINT 'Added index for IPOId';
END

COMMIT TRANSACTION;

PRINT '';
PRINT '=== Column Addition Complete ===';
PRINT 'Verify columns:';

SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'IPO_GroupMaster'
ORDER BY ORDINAL_POSITION;
