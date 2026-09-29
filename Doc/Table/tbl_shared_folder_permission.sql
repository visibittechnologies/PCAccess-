-- =============================================================================
-- TABLE: tbl_shared_folder_permission
-- AUTHOR: Antigravity / Senior .NET Developer
-- DATE: 2026-09-22
-- PURPOSE: Stores granular user permissions for each shared folder.
-- REASON & ARCHITECTURAL PATTERN:
--   - Maps Folder <-> User permissions: CanView, CanDownload, CanUpload, CanDelete.
--   - Enforced server-side in API controllers.
--   - Unique constraint ensures 1 permission record per user per shared folder.
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_shared_folder_permission')
BEGIN
    CREATE TABLE tbl_shared_folder_permission
    (
        permission_id   BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        folder_id       BIGINT NOT NULL,
        user_id         BIGINT NOT NULL,
        can_view        BIT NOT NULL DEFAULT 1,
        can_download    BIT NOT NULL DEFAULT 0,
        can_upload      BIT NOT NULL DEFAULT 0,
        can_delete      BIT NOT NULL DEFAULT 0,
        created_date    DATETIME NOT NULL DEFAULT GETDATE(),
        modified_date   DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_folder_perm_folder FOREIGN KEY (folder_id) 
            REFERENCES tbl_shared_folder(folder_id) ON DELETE CASCADE,
        CONSTRAINT FK_folder_perm_user FOREIGN KEY (user_id) 
            REFERENCES tbl_user(user_id) ON DELETE CASCADE,
        CONSTRAINT UQ_folder_user_perm UNIQUE (folder_id, user_id)
    );

    CREATE INDEX IX_folder_perm_folder ON tbl_shared_folder_permission(folder_id);
    CREATE INDEX IX_folder_perm_user ON tbl_shared_folder_permission(user_id);

    PRINT 'tbl_shared_folder_permission created successfully.';
END
ELSE
BEGIN
    PRINT 'tbl_shared_folder_permission already exists.';
END
