-- Migration: Add API Error Logging Table
-- Optimized for error-only logging (4xx, 5xx status codes)
-- Run this script manually on your database

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IPO_ApiLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[IPO_ApiLogs] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Method] NVARCHAR(10) NULL,
        [Path] NVARCHAR(500) NULL,
        [QueryString] NVARCHAR(2000) NULL,
        [StatusCode] INT NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [UserId] INT NULL,
        [IpAddress] NVARCHAR(45) NULL,
        [RequestTime] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [DurationMs] BIGINT NOT NULL DEFAULT 0
    );

    -- Create index on RequestTime for faster cleanup queries
    CREATE INDEX IX_IPO_ApiLogs_RequestTime ON [dbo].[IPO_ApiLogs] ([RequestTime]);

    -- Create index on StatusCode for faster error analysis
    CREATE INDEX IX_IPO_ApiLogs_StatusCode ON [dbo].[IPO_ApiLogs] ([StatusCode]);

    PRINT 'IPO_ApiLogs table created successfully (optimized for error-only logging)';
END
ELSE
BEGIN
    PRINT 'IPO_ApiLogs table already exists';
END
GO

-- Note: RequestBody and ResponseBody columns removed for performance
-- This design only stores error metadata, not full request/response payloads
