-- =============================================================================
-- STORED PROCEDURE: proc_shared_folder_manager
-- AUTHOR: Antigravity / Senior .NET Developer
-- DATE: 2026-09-22
-- PURPOSE: Manages CRUD operations and permission assignments for shared folders.
-- REASON & ARCHITECTURAL PATTERN:
--   - Strict multi-tenant security: ALWAYS validates device ownership against @user_id.
--   - Uses standard OUTPUT parameters: @status VARCHAR(20) and @msg NVARCHAR(500).
--   - Zero physical file deletion: only metadata and access rules are altered.
--   - Automatically grants the folder creator full permissions upon creation.
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE proc_shared_folder_manager
(
    @ActionType         INT,
    @user_id            BIGINT          = NULL,
    @device_guid        VARCHAR(50)     = NULL,
    @device_id          INT             = NULL,
    @folder_id          BIGINT          = NULL,
    @folder_guid        VARCHAR(50)     = NULL,
    @folder_name        NVARCHAR(150)   = NULL,
    @local_path         NVARCHAR(500)   = NULL,
    @description        NVARCHAR(500)   = NULL,
    @is_active          BIT             = 1,
    @target_user_id     BIGINT          = NULL,
    @can_view           BIT             = 1,
    @can_download       BIT             = 0,
    @can_upload         BIT             = 0,
    @can_delete         BIT             = 0,
    @status             VARCHAR(20)     OUTPUT,
    @msg                NVARCHAR(500)   OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @status = 'failed';
    SET @msg = 'Invalid ActionType specified.';

    BEGIN TRY
        -- ---------------------------------------------------------------------
        -- Helper: Resolve device_id if device_guid is passed
        -- ---------------------------------------------------------------------
        IF @device_id IS NULL AND @device_guid IS NOT NULL
        BEGIN
            SELECT @device_id = id FROM tbl_device WHERE device_guid = @device_guid AND is_active = 1;
        END

        -- =====================================================================
        -- ACTION 1: ADD NEW SHARED FOLDER
        -- =====================================================================
        IF @ActionType = 1
        BEGIN
            -- 1. Validate device exists and belongs to requesting user
            IF NOT EXISTS (SELECT 1 FROM tbl_device WHERE id = @device_id AND user_id = @user_id AND is_active = 1)
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Device not found or you do not have permission to manage this device.';
                RETURN;
            END

            -- 2. Validate folder name and path are provided
            IF @folder_name IS NULL OR LTRIM(RTRIM(@folder_name)) = ''
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Folder name is required.';
                RETURN;
            END

            IF @local_path IS NULL OR LTRIM(RTRIM(@local_path)) = ''
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Local path is required.';
                RETURN;
            END

            -- Clean up trailing slashes for standard consistency
            SET @local_path = RTRIM(@local_path);
            IF LEN(@local_path) > 3 AND (RIGHT(@local_path, 1) = '\' OR RIGHT(@local_path, 1) = '/')
            BEGIN
                SET @local_path = SUBSTRING(@local_path, 1, LEN(@local_path) - 1);
            END

            -- 3. Check for duplicate active folder on the same device
            IF EXISTS (SELECT 1 FROM tbl_shared_folder WHERE device_id = @device_id AND local_path = @local_path AND is_active = 1)
            BEGIN
                SET @status = 'failed';
                SET @msg = 'This local folder is already registered as a shared folder on this device.';
                RETURN;
            END

            -- 4. Insert shared folder record
            DECLARE @newFolderId BIGINT;
            DECLARE @newFolderGuid UNIQUEIDENTIFIER = NEWID();

            INSERT INTO tbl_shared_folder (folder_guid, device_id, folder_name, local_path, description, is_active, created_by, created_date, modified_date)
            VALUES (@newFolderGuid, @device_id, LTRIM(RTRIM(@folder_name)), @local_path, @description, 1, @user_id, GETDATE(), GETDATE());

            SET @newFolderId = SCOPE_IDENTITY();

            -- 5. Auto-grant folder creator full permissions
            INSERT INTO tbl_shared_folder_permission (folder_id, user_id, can_view, can_download, can_upload, can_delete, created_date, modified_date)
            VALUES (@newFolderId, @user_id, 1, 1, 1, 1, GETDATE(), GETDATE());

            SET @status = 'success';
            SET @msg = 'Shared folder added successfully.';

            SELECT 
                folder_id,
                folder_guid,
                device_id,
                folder_name,
                local_path,
                description,
                is_active
            FROM tbl_shared_folder
            WHERE folder_id = @newFolderId;
            RETURN;
        END

        -- =====================================================================
        -- ACTION 2: UPDATE SHARED FOLDER
        -- =====================================================================
        IF @ActionType = 2
        BEGIN
            -- 1. Validate folder exists and user owns the parent device
            IF NOT EXISTS (
                SELECT 1 
                FROM tbl_shared_folder f
                INNER JOIN tbl_device d ON f.device_id = d.id
                WHERE f.folder_id = @folder_id AND d.user_id = @user_id
            )
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Folder not found or you do not have permission to modify it.';
                RETURN;
            END

            -- 2. Update record
            UPDATE tbl_shared_folder
            SET 
                folder_name = ISNULL(LTRIM(RTRIM(@folder_name)), folder_name),
                description = ISNULL(@description, description),
                is_active = ISNULL(@is_active, is_active),
                modified_date = GETDATE()
            WHERE folder_id = @folder_id;

            SET @status = 'success';
            SET @msg = 'Shared folder updated successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 3: REMOVE / DEACTIVATE SHARED FOLDER (Soft Delete)
        -- =====================================================================
        IF @ActionType = 3
        BEGIN
            -- 1. Validate folder ownership
            IF NOT EXISTS (
                SELECT 1 
                FROM tbl_shared_folder f
                INNER JOIN tbl_device d ON f.device_id = d.id
                WHERE f.folder_id = @folder_id AND d.user_id = @user_id
            )
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Folder not found or unauthorized.';
                RETURN;
            END

            -- 2. Deactivate folder (physical files remain untouched on user PC)
            UPDATE tbl_shared_folder
            SET 
                is_active = 0,
                modified_date = GETDATE()
            WHERE folder_id = @folder_id;

            SET @status = 'success';
            SET @msg = 'Shared folder removed successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 4: GET SHARED FOLDERS FOR A DEVICE
        -- =====================================================================
        IF @ActionType = 4
        BEGIN
            SELECT 
                f.folder_id,
                f.folder_guid,
                f.device_id,
                d.device_name,
                d.device_guid,
                d.status AS device_status,
                f.folder_name,
                f.local_path,
                f.description,
                f.is_active,
                f.created_date,
                f.modified_date,
                (SELECT COUNT(1) FROM tbl_shared_folder_permission p WHERE p.folder_id = f.folder_id) AS permission_count
            FROM tbl_shared_folder f
            INNER JOIN tbl_device d ON f.device_id = d.id
            WHERE f.device_id = @device_id 
              AND d.user_id = @user_id
              AND f.is_active = 1
            ORDER BY f.folder_id DESC;

            SET @status = 'success';
            SET @msg = 'Folders retrieved successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 5: GET PERMISSIONS FOR A SHARED FOLDER
        -- =====================================================================
        IF @ActionType = 5
        BEGIN
            -- Validate ownership of parent device
            IF NOT EXISTS (
                SELECT 1 
                FROM tbl_shared_folder f
                INNER JOIN tbl_device d ON f.device_id = d.id
                WHERE f.folder_id = @folder_id AND d.user_id = @user_id
            )
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Folder not found or unauthorized.';
                RETURN;
            END

            -- Return all active users and their current permissions on this folder
            SELECT 
                u.user_id,
                u.name AS user_display_name,
                u.user_name,
                u.email,
                ISNULL(p.permission_id, 0) AS permission_id,
                ISNULL(p.can_view, 0) AS can_view,
                ISNULL(p.can_download, 0) AS can_download,
                ISNULL(p.can_upload, 0) AS can_upload,
                ISNULL(p.can_delete, 0) AS can_delete,
                CASE WHEN u.user_id = @user_id THEN 1 ELSE 0 END AS is_owner
            FROM tbl_user u
            LEFT JOIN tbl_shared_folder_permission p 
                ON u.user_id = p.user_id AND p.folder_id = @folder_id
            WHERE u.status = 'Active' AND u.is_deleted = 0
            ORDER BY is_owner DESC, u.name ASC;

            SET @status = 'success';
            SET @msg = 'Permissions retrieved successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 6: UPSERT PERMISSION FOR A TARGET USER
        -- =====================================================================
        IF @ActionType = 6
        BEGIN
            -- Validate folder ownership
            IF NOT EXISTS (
                SELECT 1 
                FROM tbl_shared_folder f
                INNER JOIN tbl_device d ON f.device_id = d.id
                WHERE f.folder_id = @folder_id AND d.user_id = @user_id
            )
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Folder not found or unauthorized.';
                RETURN;
            END

            IF @target_user_id IS NULL
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Target user ID is required.';
                RETURN;
            END

            -- Merge / Upsert permission record
            IF EXISTS (SELECT 1 FROM tbl_shared_folder_permission WHERE folder_id = @folder_id AND user_id = @target_user_id)
            BEGIN
                UPDATE tbl_shared_folder_permission
                SET 
                    can_view = @can_view,
                    can_download = @can_download,
                    can_upload = @can_upload,
                    can_delete = @can_delete,
                    modified_date = GETDATE()
                WHERE folder_id = @folder_id AND user_id = @target_user_id;
            END
            ELSE
            BEGIN
                INSERT INTO tbl_shared_folder_permission (folder_id, user_id, can_view, can_download, can_upload, can_delete, created_date, modified_date)
                VALUES (@folder_id, @target_user_id, @can_view, @can_download, @can_upload, @can_delete, GETDATE(), GETDATE());
            END

            SET @status = 'success';
            SET @msg = 'Permission updated successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 7: SEED SAMPLE TEAM USERS (Rahul, Amit, Manager)
        -- =====================================================================
        IF @ActionType = 7
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM tbl_user WHERE user_name = 'rahul')
            BEGIN
                INSERT INTO tbl_user (user_type_id, user_role_id, user_name, password, name, email, status, is_deleted, created_by, created_date, modified_by, modified_date)
                VALUES (2, 2, 'rahul', '123', 'Rahul Sharma', 'rahul@pcaccess.com', 'Active', 0, 1, GETDATE(), 1, GETDATE());
            END
            ELSE
            BEGIN
                UPDATE tbl_user SET user_type_id = 2, user_role_id = 2 WHERE user_name = 'rahul';
            END

            IF NOT EXISTS (SELECT 1 FROM tbl_user WHERE user_name = 'amit')
            BEGIN
                INSERT INTO tbl_user (user_type_id, user_role_id, user_name, password, name, email, status, is_deleted, created_by, created_date, modified_by, modified_date)
                VALUES (2, 2, 'amit', '123', 'Amit Verma', 'amit@pcaccess.com', 'Active', 0, 1, GETDATE(), 1, GETDATE());
            END
            ELSE
            BEGIN
                UPDATE tbl_user SET user_type_id = 2, user_role_id = 2 WHERE user_name = 'amit';
            END

            IF NOT EXISTS (SELECT 1 FROM tbl_user WHERE user_name = 'manager')
            BEGIN
                INSERT INTO tbl_user (user_type_id, user_role_id, user_name, password, name, email, status, is_deleted, created_by, created_date, modified_by, modified_date)
                VALUES (2, 2, 'manager', '123', 'Project Manager', 'manager@pcaccess.com', 'Active', 0, 1, GETDATE(), 1, GETDATE());
            END
            ELSE
            BEGIN
                UPDATE tbl_user SET user_type_id = 2, user_role_id = 2 WHERE user_name = 'manager';
            END

            SET @status = 'success';
            SET @msg = 'Sample team users seeded successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 8: GET ALL ACTIVE TEAM USERS
        -- =====================================================================
        IF @ActionType = 8
        BEGIN
            SELECT 
                user_id,
                user_name,
                name AS user_display_name,
                email,
                status
            FROM tbl_user
            WHERE status = 'Active' AND is_deleted = 0
            ORDER BY user_id ASC;

            SET @status = 'success';
            SET @msg = 'Active team users retrieved successfully.';
            RETURN;
        END

    
        -- =====================================================================
        -- ACTION 9: GET FOLDER DETAILS & VERIFY VIEW PERMISSION (Step 4 File Browser)
        -- =====================================================================
        IF @ActionType = 9
        BEGIN
            -- 1. Validate folder exists and is active
            IF NOT EXISTS (
                SELECT 1 FROM tbl_shared_folder WHERE folder_guid = @folder_guid AND is_active = 1
            )
            BEGIN
                SET @status = 'failed';
                SET @msg = 'Shared folder not found or has been deactivated.';
                RETURN;
            END

            -- 2. Check view permissions: Device Owner OR Explicit CanView in tbl_shared_folder_permission
            DECLARE @is_owner BIT = 0;
            SET @can_view = 0;
            SET @can_download = 0;
            SET @can_upload = 0;
            SET @can_delete = 0;

            -- Check if user is device owner
            IF EXISTS (
                SELECT 1 
                FROM tbl_shared_folder f
                INNER JOIN tbl_device d ON f.device_id = d.id
                WHERE f.folder_guid = @folder_guid AND d.user_id = @user_id
            )
            BEGIN
                SET @is_owner = 1;
                SET @can_view = 1;
                SET @can_download = 1;
                SET @can_upload = 1;
                SET @can_delete = 1;
            END
            ELSE
            BEGIN
                -- Check explicit permission
                SELECT 
                    @can_view = ISNULL(p.can_view, 0),
                    @can_download = ISNULL(p.can_download, 0),
                    @can_upload = ISNULL(p.can_upload, 0),
                    @can_delete = ISNULL(p.can_delete, 0)
                FROM tbl_shared_folder f
                INNER JOIN tbl_shared_folder_permission p ON f.folder_id = p.folder_id
                WHERE f.folder_guid = @folder_guid AND p.user_id = @user_id;

                IF @can_view = 0
                BEGIN
                    SET @status = 'failed';
                    SET @msg = 'Access Denied: You do not have permission to view this shared folder.';
                    RETURN;
                END
            END

            -- 3. Return folder details along with device status & permission flags
            SELECT 
                f.folder_id,
                f.folder_guid,
                f.folder_name,
                f.local_path,
                f.description,
                f.is_active,
                f.created_date,
                d.id AS device_id,
                d.device_guid,
                d.device_name,
                d.status AS device_status,
                d.last_seen,
                CASE 
                    WHEN d.status = 'online' AND DATEDIFF(SECOND, ISNULL(d.last_seen, '2000-01-01'), GETDATE()) <= 90 THEN 1 
                    ELSE 0 
                END AS is_device_online,
                @is_owner AS is_owner,
                @can_view AS can_view,
                @can_download AS can_download,
                @can_upload AS can_upload,
                @can_delete AS can_delete
            FROM tbl_shared_folder f
            INNER JOIN tbl_device d ON f.device_id = d.id
            WHERE f.folder_guid = @folder_guid;

            SET @status = 'success';
            SET @msg = 'Folder details retrieved successfully.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 10: GET ALL SHARED FOLDERS FOR USER (ACROSS ALL DEVICES)
        -- REASON: Powers the DashboardOverview Shared Folders card & device counts.
        --         Unlike ActionType 4 (single device), this fetches all devices.
        -- =====================================================================
        IF @ActionType = 10
        BEGIN
            SELECT 
                f.folder_id,
                f.folder_guid,
                f.device_id,
                d.device_name,
                d.device_guid,
                d.status AS device_status,
                f.folder_name,
                f.local_path,
                f.description,
                f.is_active,
                f.created_date,
                f.modified_date,
                (SELECT COUNT(1) FROM tbl_shared_folder_permission p WHERE p.folder_id = f.folder_id) AS permission_count
            FROM tbl_shared_folder f
            INNER JOIN tbl_device d ON f.device_id = d.id
            WHERE d.user_id = @user_id
              AND f.is_active = 1
              AND d.is_active = 1
            ORDER BY f.folder_id DESC;

            SET @status = 'success';
            SET @msg = 'All user folders retrieved successfully.';
            RETURN;
        END

    END TRY
    BEGIN CATCH
        SET @status = 'error';
        SET @msg = ERROR_MESSAGE();
    END CATCH
END;
GO
