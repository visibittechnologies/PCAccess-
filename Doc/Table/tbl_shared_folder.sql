SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================================
-- TABLE: tbl_shared_folder
-- AUTHOR: Antigravity / Senior .NET Developer
-- DATE: 2026-09-22
-- PURPOSE: Stores metadata and local paths of folders shared from a registered PC.
-- REASON & ARCHITECTURAL PATTERN:
--   - Device -> Shared Folder 1-to-many relationship.
--   - Stores ONLY metadata (folder_name, local_path, description).
--   - Zero physical files stored on the server.
--   - Unique constraint prevents duplicate active folder registration on same PC.
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_shared_folder')
BEGIN
    CREATE TABLE tbl_shared_folder
    (
        folder_id       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        folder_guid     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        device_id       INT NOT NULL,
        folder_name     NVARCHAR(150) NOT NULL,
        local_path      NVARCHAR(500) NOT NULL,
        description     NVARCHAR(500) NULL,
        is_active       BIT NOT NULL DEFAULT 1,
        created_by      BIGINT NOT NULL,
        created_date    DATETIME NOT NULL DEFAULT GETDATE(),
        modified_date   DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_shared_folder_device FOREIGN KEY (device_id) 
            REFERENCES tbl_device(id) ON DELETE CASCADE,
        CONSTRAINT FK_shared_folder_user FOREIGN KEY (created_by) 
            REFERENCES tbl_user(user_id)
    );

    CREATE UNIQUE INDEX UQ_shared_folder_device_path 
        ON tbl_shared_folder(device_id, local_path) 
        WHERE is_active = 1;

    CREATE INDEX IX_shared_folder_device_active 
        ON tbl_shared_folder(device_id, is_active);

    PRINT 'tbl_shared_folder created successfully.';
END
ELSE
BEGIN
    PRINT 'tbl_shared_folder already exists.';
END
