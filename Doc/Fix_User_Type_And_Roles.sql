USE [PCAccess_DB]
GO

-- =============================================================================
-- SCRIPT: Fix_User_Type_And_Roles.sql
-- PURPOSE: 
--   1. Ensures tbl_user_type has: user_type_id 1 = 'Admin' and 2 = 'User'.
--   2. Ensures tbl_user_role has: user_role_id 1 = 'Admin' and 2 = 'User'.
--   3. Updates tbl_user:
--      - Admin (admin): user_type_id = 1, user_role_id = 1
--      - Team Users (rahul, amit, etc.): user_type_id = 2, user_role_id = 2
--   4. Updates proc_user_agent_auth to return proper mapping.
-- =============================================================================

PRINT '---------------------------------------------------------';
PRINT 'STEP 1: Setting up tbl_user_type (1 = Admin, 2 = User)...';
PRINT '---------------------------------------------------------';

-- 1. Ensure user_type_id 1 is Admin in tbl_user_type
IF NOT EXISTS (SELECT 1 FROM tbl_user_type WHERE user_type_id = 1)
BEGIN
    INSERT INTO tbl_user_type (user_type_id, user_type, user_level, module_url)
    VALUES (1, 'Admin', 1, '/Admin/DashboardOverview');
    PRINT 'Inserted user_type_id 1 = Admin into tbl_user_type';
END
ELSE
BEGIN
    UPDATE tbl_user_type 
    SET user_type = 'Admin', module_url = '/Admin/DashboardOverview' 
    WHERE user_type_id = 1;
    PRINT 'Updated user_type_id 1 = Admin in tbl_user_type';
END

-- 2. Ensure user_type_id 2 is User in tbl_user_type
IF NOT EXISTS (SELECT 1 FROM tbl_user_type WHERE user_type_id = 2)
BEGIN
    INSERT INTO tbl_user_type (user_type_id, user_type, user_level, module_url)
    VALUES (2, 'User', 2, '/Admin/DashboardOverview');
    PRINT 'Inserted user_type_id 2 = User into tbl_user_type';
END
ELSE
BEGIN
    UPDATE tbl_user_type 
    SET user_type = 'User'
    WHERE user_type_id = 2;
    PRINT 'Updated user_type_id 2 = User in tbl_user_type';
END

PRINT '---------------------------------------------------------';
PRINT 'STEP 2: Setting up tbl_user_role (1 = Admin, 2 = User)...';
PRINT '---------------------------------------------------------';

-- 1. Ensure user_role_id 1 is Admin in tbl_user_role
IF NOT EXISTS (SELECT 1 FROM tbl_user_role WHERE user_role_id = 1)
BEGIN
    INSERT INTO tbl_user_role (user_role_id, user_role_name, is_system, created_on)
    VALUES (1, 'Admin', 1, GETDATE());
    PRINT 'Inserted user_role_id 1 = Admin into tbl_user_role';
END
ELSE
BEGIN
    UPDATE tbl_user_role 
    SET user_role_name = 'Admin' 
    WHERE user_role_id = 1;
    PRINT 'Updated user_role_id 1 = Admin in tbl_user_role';
END

-- 2. Ensure user_role_id 2 is User in tbl_user_role
IF NOT EXISTS (SELECT 1 FROM tbl_user_role WHERE user_role_id = 2)
BEGIN
    INSERT INTO tbl_user_role (user_role_id, user_role_name, is_system, created_on)
    VALUES (2, 'User', 0, GETDATE());
    PRINT 'Inserted user_role_id 2 = User into tbl_user_role';
END
ELSE
BEGIN
    UPDATE tbl_user_role 
    SET user_role_name = 'User' 
    WHERE user_role_id = 2;
    PRINT 'Updated user_role_id 2 = User in tbl_user_role';
END

PRINT '---------------------------------------------------------';
PRINT 'STEP 3: Updating tbl_user Mapping (user_type_id & user_role_id)...';
PRINT '---------------------------------------------------------';

-- Admin user: user_type_id = 1, user_role_id = 1
UPDATE tbl_user
SET user_type_id = 1,
    user_role_id = 1
WHERE user_name = 'admin';

PRINT 'Updated admin to user_type_id = 1, user_role_id = 1';

-- Non-admin users (rahul, amit, manager, etc.): user_type_id = 2, user_role_id = 2
UPDATE tbl_user
SET user_type_id = 2,
    user_role_id = 2
WHERE user_name IN ('rahul', 'amit', 'manager')
   OR (user_name <> 'admin' AND user_type_id = 1);

PRINT 'Updated non-admin users to user_type_id = 2, user_role_id = 2';

-- Verification Results
SELECT 
    u.user_id,
    u.user_name,
    u.name,
    u.email,
    u.user_type_id,
    ut.user_type,
    u.user_role_id,
    ur.user_role_name,
    u.status
FROM tbl_user u
LEFT JOIN tbl_user_type ut ON u.user_type_id = ut.user_type_id
LEFT JOIN tbl_user_role ur ON u.user_role_id = ur.user_role_id;

GO

-- =============================================================================
-- STEP 4: Update proc_user_agent_auth
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

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
        -- 1. Validate Input
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

        -- 2. User Existence Check
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

        -- 3. Password Verification
        IF @actual_pass <> @password
        BEGIN
            SET @status = 'failed';
            SET @msg = 'Invalid password.';
            RETURN;
        END

        -- Determine role name strictly: user_type_id = 1 -> 'Admin', user_type_id = 2 -> 'User'
        IF @user_type_id = 1
        BEGIN
            SET @role_name = 'Admin';
        END
        ELSE
        BEGIN
            SET @role_name = 'User';
        END

        -- 4. Result Set 1: User Profile Details (with both user_type_id and user_role_id)
        SELECT 
            @user_id        AS user_id,
            @user_name      AS user_name,
            @name           AS name,
            @email          AS email,
            @user_type_id   AS user_type_id,
            @user_role_id   AS user_role_id,
            @role_name      AS role_name,
            NEWID()         AS session_token;

        -- 5. Result Set 2: Permitted Shared Folders
        IF @user_type_id = 1
        BEGIN
            -- Admin has full access to all active folders across all devices
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
            -- User (user_type_id = 2): filter by device ownership OR explicit permission in tbl_shared_folder_permission
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

PRINT 'proc_user_agent_auth updated successfully.';
GO
