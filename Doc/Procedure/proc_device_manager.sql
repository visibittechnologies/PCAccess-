USE [PCAccess_DB]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_device_manager')
    DROP PROCEDURE [dbo].[proc_device_manager]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
================================================================================
PROCEDURE: [dbo].[proc_device_manager]
AUTHOR: Antigravity / Senior .NET Developer
DATE: 2026-09-22
PURPOSE:
  Centralized procedure for all device registration, pairing, status tracking,
  heartbeat updates, and dashboard list retrieval.

REASON & ARCHITECTURAL PATTERN:
  - Strictly follows the project's standard Stored Procedure pattern:
    @ActionType, input parameters, and @status + @msg OUTPUT parameters.
  - Encapsulated inside BEGIN TRY ... BEGIN TRANSACTION ... COMMIT ... END TRY
    BEGIN CATCH ... ROLLBACK ... END CATCH blocks.
  - Matches the DAL execution pattern (MySqlHelper.ExecuteDataTable).

ACTION TYPES:
  @ActionType = 1 -> Create / Save new 6-digit Pairing Code for User
  @ActionType = 2 -> Validate Pairing Code & Register New Device (issues persistent token)
  @ActionType = 3 -> Authenticate Existing Device via DeviceGuid + Token
  @ActionType = 4 -> Update Device Online/Offline Status and ConnectionId
  @ActionType = 5 -> Record Heartbeat (updates last_seen)
  @ActionType = 6 -> Get User Devices List for Dashboard
  @ActionType = 7 -> Mark Stale Devices Offline (> 90 seconds without heartbeat)
================================================================================
*/

CREATE PROCEDURE [dbo].[proc_device_manager]
    @ActionType INT,
    @user_id BIGINT = NULL,
    @device_guid UNIQUEIDENTIFIER = NULL,
    @device_name NVARCHAR(150) = NULL,
    @device_token NVARCHAR(256) = NULL,
    @pairing_code VARCHAR(10) = NULL,
    @status_val VARCHAR(20) = NULL,
    @connection_id NVARCHAR(100) = NULL,
    @status VARCHAR(50) OUTPUT,
    @msg NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- =====================================================================
        -- ACTION 1: Generate / Save 6-digit Pairing Code for User
        -- =====================================================================
        IF (@ActionType = 1)
        BEGIN
            IF (@user_id IS NULL OR @pairing_code IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'User ID and Pairing Code are required.';
                RETURN;
            END

            -- Invalidate any previous unused codes for this user
            UPDATE [dbo].[tbl_device_pairing]
            SET [is_used] = 1
            WHERE [user_id] = @user_id AND [is_used] = 0;

            -- Insert new code with 10-minute expiry
            INSERT INTO [dbo].[tbl_device_pairing] ([pairing_code], [user_id], [expires_at], [is_used], [created_at])
            VALUES (@pairing_code, @user_id, DATEADD(MINUTE, 10, GETDATE()), 0, GETDATE());

            SET @status = 'success';
            SET @msg = 'Pairing code generated successfully.';

            SELECT @pairing_code AS pairing_code, DATEADD(MINUTE, 10, GETDATE()) AS expires_at;
            RETURN;
        END

        -- =====================================================================
        -- ACTION 2: Validate Pairing Code & Register New Device
        -- =====================================================================
        IF (@ActionType = 2)
        BEGIN
            IF (@pairing_code IS NULL OR @device_guid IS NULL OR @device_name IS NULL OR @device_token IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'Pairing code, DeviceGuid, DeviceName, and Token are required.';
                RETURN;
            END

            DECLARE @TargetUserId BIGINT = NULL;
            DECLARE @PairingId INT = NULL;

            SELECT TOP 1 
                @PairingId = [id],
                @TargetUserId = [user_id]
            FROM [dbo].[tbl_device_pairing]
            WHERE [pairing_code] = @pairing_code 
              AND [is_used] = 0 
              AND [expires_at] > GETDATE();

            IF (@TargetUserId IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'Invalid or expired pairing code. Please generate a new code from Dashboard.';
                RETURN;
            END

            -- Mark pairing code as used
            UPDATE [dbo].[tbl_device_pairing]
            SET [is_used] = 1
            WHERE [id] = @PairingId;

            -- Check if device already exists with this GUID
            IF EXISTS (SELECT 1 FROM [dbo].[tbl_device] WHERE [device_guid] = @device_guid)
            BEGIN
                UPDATE [dbo].[tbl_device]
                SET [device_name] = @device_name,
                    [user_id] = @TargetUserId,
                    [device_token] = @device_token,
                    [status] = 'offline',
                    [updated_at] = GETDATE(),
                    [is_active] = 1
                WHERE [device_guid] = @device_guid;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[tbl_device] 
                    ([device_guid], [device_name], [user_id], [device_token], [status], [last_seen], [created_at], [is_active])
                VALUES 
                    (@device_guid, @device_name, @TargetUserId, @device_token, 'offline', GETDATE(), GETDATE(), 1);
            END

            SET @status = 'success';
            SET @msg = 'Device successfully registered and paired.';

            SELECT 
                d.[id],
                d.[device_guid],
                d.[device_name],
                d.[user_id],
                d.[device_token],
                d.[status],
                d.[last_seen]
            FROM [dbo].[tbl_device] d
            WHERE d.[device_guid] = @device_guid;

            RETURN;
        END

        -- =====================================================================
        -- ACTION 3: Authenticate Existing Device (DeviceGuid + DeviceToken)
        -- =====================================================================
        IF (@ActionType = 3)
        BEGIN
            IF (@device_guid IS NULL OR @device_token IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'DeviceGuid and Token are required.';
                RETURN;
            END

            IF EXISTS (
                SELECT 1 FROM [dbo].[tbl_device] 
                WHERE [device_guid] = @device_guid 
                  AND [device_token] = @device_token 
                  AND [is_active] = 1
            )
            BEGIN
                SET @status = 'success';
                SET @msg = 'Device authenticated successfully.';

                SELECT 
                    [id],
                    [device_guid],
                    [device_name],
                    [user_id],
                    [status],
                    [last_seen]
                FROM [dbo].[tbl_device]
                WHERE [device_guid] = @device_guid;
            END
            ELSE
            BEGIN
                SET @status = 'error';
                SET @msg = 'Invalid device credentials. Re-registration required.';
            END
            RETURN;
        END

        -- =====================================================================
        -- ACTION 4: Update Device Status & ConnectionId
        -- =====================================================================
        IF (@ActionType = 4)
        BEGIN
            IF (@device_guid IS NULL OR @status_val IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'DeviceGuid and Status are required.';
                RETURN;
            END

            UPDATE [dbo].[tbl_device]
            SET [status] = @status_val,
                [connection_id] = CASE WHEN @status_val = 'offline' THEN NULL ELSE ISNULL(@connection_id, [connection_id]) END,
                [last_seen] = GETDATE(),
                [updated_at] = GETDATE()
            WHERE [device_guid] = @device_guid;

            SET @status = 'success';
            SET @msg = 'Status updated successfully.';

            -- Return updated device data and user_id (for SignalR dashboard group broadcast)
            SELECT 
                d.[id],
                d.[device_guid],
                d.[device_name],
                d.[user_id],
                d.[status],
                d.[last_seen]
            FROM [dbo].[tbl_device] d
            WHERE d.[device_guid] = @device_guid;

            RETURN;
        END

        -- =====================================================================
        -- ACTION 5: Record Heartbeat
        -- =====================================================================
        IF (@ActionType = 5)
        BEGIN
            IF (@device_guid IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'DeviceGuid is required.';
                RETURN;
            END

            UPDATE [dbo].[tbl_device]
            SET [last_seen] = GETDATE(),
                [status] = 'online'
            WHERE [device_guid] = @device_guid;

            SET @status = 'success';
            SET @msg = 'Heartbeat recorded.';
            RETURN;
        END

        -- =====================================================================
        -- ACTION 6: Get User Devices List for Dashboard
        -- =====================================================================
        IF (@ActionType = 6)
        BEGIN
            IF (@user_id IS NULL)
            BEGIN
                SET @status = 'error';
                SET @msg = 'User ID is required.';
                RETURN;
            END

            SET @status = 'success';
            SET @msg = 'Device list retrieved.';

            SELECT 
                d.[id],
                d.[device_guid],
                d.[device_name],
                d.[user_id],
                d.[status],
                d.[last_seen],
                d.[created_at],
                -- Calculate friendly relative last seen or status
                DATEDIFF(SECOND, d.[last_seen], GETDATE()) AS seconds_since_last_seen
            FROM [dbo].[tbl_device] d
            WHERE d.[user_id] = @user_id AND d.[is_active] = 1
            ORDER BY 
                CASE WHEN d.[status] = 'online' THEN 0 ELSE 1 END,
                d.[last_seen] DESC;

            RETURN;
        END

        -- =====================================================================
        -- ACTION 7: Mark Stale Devices Offline (> 90 seconds without heartbeat)
        -- =====================================================================
        IF (@ActionType = 7)
        BEGIN
            UPDATE [dbo].[tbl_device]
            SET [status] = 'offline',
                [connection_id] = NULL,
                [updated_at] = GETDATE()
            WHERE [status] = 'online' 
              AND DATEDIFF(SECOND, [last_seen], GETDATE()) > 90;

            SET @status = 'success';
            SET @msg = 'Stale devices marked offline.';
            RETURN;
        END

        -- Default fallback
        SET @status = 'error';
        SET @msg = 'Invalid ActionType.';
    END TRY
    BEGIN CATCH
        SET @status = 'error';
        SET @msg = ERROR_MESSAGE();
    END CATCH
END
GO
