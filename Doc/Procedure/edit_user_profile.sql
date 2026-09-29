USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'edit_user_profile')
DROP PROCEDURE [dbo].[edit_user_profile]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[edit_user_profile]
(
    @flag NVARCHAR(50),

    @user_id BIGINT = NULL,
    @username NVARCHAR(100) = NULL,
    @password NVARCHAR(200) = NULL,
    @name NVARCHAR(200) = NULL,
    @email NVARCHAR(200) = NULL,
    @profile_photo NVARCHAR(500) = NULL,

    @created_by BIGINT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    IF (@flag = 'get_user_detail')
    BEGIN
        SELECT
            user_id,user_name,password,name,email,profile_photo FROM tbl_user
        WHERE user_id = @user_id AND is_deleted = 0;
    END
    ELSE IF (@flag = 'update_profile_image')
    BEGIN
        UPDATE tbl_user
        SET
            profile_photo = @profile_photo,
            modified_by = @created_by,
            modified_date = GETDATE()
        WHERE user_id = @user_id;
        SELECT 'success' AS response, 'Profile image updated successfully' AS message;
    END
    ELSE IF (@flag = 'profile_update')
    BEGIN
        UPDATE tbl_user
        SET
            user_name = CASE WHEN @username IS NOT NULL AND @username <> '' THEN @username ELSE user_name END,
            password  = CASE WHEN @password IS NOT NULL AND @password <> '' THEN @password ELSE password END,
            name       = CASE WHEN @name IS NOT NULL AND @name <> '' THEN @name ELSE name END,
            email      = CASE WHEN @email IS NOT NULL AND @email <> '' THEN @email ELSE email END,
            modified_by = @created_by,
            modified_date = GETDATE()
        WHERE user_id = @user_id;
        SELECT 'success' AS response, 'Profile updated successfully' AS message;
    END
END

GO

