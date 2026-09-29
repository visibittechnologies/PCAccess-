USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_add_seo_tool')
DROP PROCEDURE [dbo].[proc_add_seo_tool]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[proc_add_seo_tool]
    @flag NVARCHAR(100),
    @id INT = NULL OUTPUT, 
    @user_id BIGINT = 0,
    @googleScript NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @flag = 'insert'
    BEGIN
        INSERT INTO tbl_google_script (ScriptContent, is_deleted, created_by, created_date)
        VALUES (@googleScript, 0, @user_id, GETUTCDATE());

        SET @id = SCOPE_IDENTITY();  -- Only return ID in case of insert

        SELECT @id AS Id, ScriptContent FROM tbl_google_script WHERE Id = @id;
    END
    ELSE IF @flag = 'update'
    BEGIN
        UPDATE tbl_google_script 
        SET ScriptContent = @googleScript,
            modified_by = @user_id,
            updated_date = GETUTCDATE()
        WHERE Id = @id;

        SELECT Id, ScriptContent FROM tbl_google_script WHERE Id = @id;
    END
    ELSE IF @flag = 'view'
    BEGIN
       SELECT Id, ScriptContent FROM tbl_google_script WHERE is_deleted=0 ORDER BY Id DESC;
    END
    ELSE IF @flag = 'get'
    BEGIN
       SELECT Id, ScriptContent FROM tbl_google_script WHERE is_deleted=0 AND Id=@id;
    END
    ELSE IF @flag = 'remove'
    BEGIN
       UPDATE tbl_google_script SET is_deleted=1 WHERE Id = @id;
	    SELECT @id AS id, 'success' AS response;
    END
END

GO

