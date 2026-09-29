USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- STORED PROCEDURE: proc_user_agent_auth
-- AUTHOR: Antigravity / Senior .NET Developer
-- DATE: 2026-09-28
-- PURPOSE: Authenticates user (Admin or Employee) for UserLoginAgent and returns
--          user profile details along with all permitted shared folders.
-- REASON & ARCHITECTURAL PATTERN:
--   - Enables direct client desktop agent access without opening web browser.
--   - Allows login via either username OR email.
--   - Multi-tenant role-aware: Admins see all active folders; Employees see only
--     folders where they are device owner OR have can_view = 1 in tbl_shared_folder_permission.
--   - Standard OUTPUT parameters: @status VARCHAR(20) and @msg NVARCHAR(500).
-- =============================================================================

CREATE OR ALTER PROCEDURE [dbo].[proc_user_agent_auth]
(
    @username_or_email  VARCHAR(350),
    @password           NVARCHAR(MAX),
    @client_machine     NVARCHAR(150)   = NULL,
    @status             VARCHAR(20)     OUTPUT,
    @msg                NVARCHAR(500)   OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @status = 'failed';
    SET @msg = 'Invalid credentials';

    BEGIN TRY
        -- ---------------------------------------------------------------------
        -- 1. Validate Input
        -- ---------------------------------------------------------------------
        IF @username_or_email IS NULL OR LTRIM(RTRIM(@username_or_email)) = ''
        BEGIN
            SET @status = 'failed';
            SET @msg = 'Username or email is required.';
            RETURN;
        END

        IF @password IS NULL OR LTRIM(RTRIM(@password)) = ''
        BEGIN
            SET @status = 'failed';
            SET @msg = 'Password is required.';
            RETURN;
        END

        -- ---------------------------------------------------------------------
        -- 2. User Existence Check (by username or email)
        -- ---------------------------------------------------------------------
        DECLARE 
            @user_id        BIGINT = NULL,
            @user_type_id   INT = NULL,
            @user_role_id   INT = NULL,
            @user_status    VARCHAR(100) = NULL,
            @is_deleted     BIT = 0,
            @actual_pass    NVARCHAR(MAX) = NULL,
            @name           NVARCHAR(350) = NULL,
            @user_name      VARCHAR(MAX) = NULL,
            @email          NVARCHAR(350) = NULL,
            @role_name      VARCHAR(100) = 'User';

        SELECT TOP 1
            @user_id        = tu.user_id,
            @user_type_id   = ISNULL(tu.user_type_id, 2),
            @user_role_id   = ISNULL(tu.user_role_id, 2),
            @user_status    = tu.[status],
            @is_deleted     = ISNULL(tu.is_deleted, 0),
            @actual_pass    = tu.[password],
            @name           = tu.name,
            @user_name      = tu.user_name,
            @email          = tu.email
        FROM tbl_user tu
        WHERE (tu.[user_name] = LTRIM(RTRIM(@username_or_email)) 
            OR tu.[email]     = LTRIM(RTRIM(@username_or_email)))
          AND ISNULL(tu.is_deleted, 0) = 0;

        IF @user_id IS NULL
        BEGIN
            SET @status = 'failed';
            SET @msg = 'User not found.';
            RETURN;
        END

        IF @user_status <> 'Active'
        BEGIN
            SET @status = 'failed';
            SET @msg = 'Your account has been deactivated. Please contact administrator.';
            RETURN;
        END

        -- ---------------------------------------------------------------------
        -- 3. Password Verification
        -- ---------------------------------------------------------------------
        IF @actual_pass <> @password
        BEGIN
            SET @status = 'failed';
            SET @msg = 'Invalid password.';
            RETURN;
        END

        -- Determine role name strictly: user_type_id = 1 is Admin, user_type_id = 2 is User
        SELECT TOP 1 @role_name = user_type FROM tbl_user_type WHERE user_type_id = @user_type_id;
        IF @role_name IS NULL OR @role_name = '' OR @user_type_id = 2 OR @user_type_id <> 1
        BEGIN
            SET @role_name = CASE WHEN @user_type_id = 1 THEN 'Admin' ELSE 'User' END;
        END

        -- ---------------------------------------------------------------------
        -- 4. Result Set 1: User Profile Details
        -- ---------------------------------------------------------------------
        SELECT 
            @user_id        AS user_id,
            @user_name      AS user_name,
            @name           AS name,
            @email          AS email,
            @user_type_id   AS user_type_id,
            @role_name      AS role_name,
            NEWID()         AS session_token;

        -- ---------------------------------------------------------------------
        -- 5. Result Set 2: Permitted Shared Folders
        --    - If Admin (user_type_id = 1): returns all active shared folders
        --    - If Employee: returns folders where user is device owner OR
        --      can_view = 1 in tbl_shared_folder_permission
        -- ---------------------------------------------------------------------
        IF @user_type_id = 1
        BEGIN
            -- Admin has full access to all active folders
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
                1 AS is_owner,
                1 AS can_view,
                1 AS can_download,
                1 AS can_upload,
                1 AS can_delete
            FROM tbl_shared_folder f
            INNER JOIN tbl_device d ON f.device_id = d.id
            WHERE f.is_active = 1
              AND d.is_active = 1
            ORDER BY d.device_name ASC, f.folder_name ASC;
        END
        ELSE
        BEGIN
            -- Employee / Standard User: filter by ownership OR explicit permission
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
                CASE WHEN d.user_id = @user_id THEN 1 ELSE 0 END AS is_owner,
                CASE WHEN d.user_id = @user_id THEN 1 ELSE ISNULL(p.can_view, 0) END AS can_view,
                CASE WHEN d.user_id = @user_id THEN 1 ELSE ISNULL(p.can_download, 0) END AS can_download,
                CASE WHEN d.user_id = @user_id THEN 1 ELSE ISNULL(p.can_upload, 0) END AS can_upload,
                CASE WHEN d.user_id = @user_id THEN 1 ELSE ISNULL(p.can_delete, 0) END AS can_delete
            FROM tbl_shared_folder f
            INNER JOIN tbl_device d ON f.device_id = d.id
            LEFT JOIN tbl_shared_folder_permission p ON f.folder_id = p.folder_id AND p.user_id = @user_id
            WHERE f.is_active = 1
              AND d.is_active = 1
              AND (d.user_id = @user_id OR ISNULL(p.can_view, 0) = 1)
            ORDER BY d.device_name ASC, f.folder_name ASC;
        END

        SET @status = 'success';
        SET @msg = 'Login successful.';
        RETURN;

    END TRY
    BEGIN CATCH
        SET @status = 'error';
        SET @msg = ERROR_MESSAGE();
    END CATCH
END;
GO
