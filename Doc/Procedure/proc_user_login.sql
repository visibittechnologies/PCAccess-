USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_user_login')
DROP PROCEDURE [dbo].[proc_user_login]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ua_userauth @username = 'admin',@password = '123',@msg = '',@status = ''
-- ua_userauth @username = 'user',@password = '123',@msg = '',@status = ''
CREATE PROCEDURE [dbo].[proc_user_login] 
(
    @username VARCHAR(MAX),
    @password VARCHAR(MAX) = NULL,
    @id VARCHAR(20) = NULL,
    @browser VARCHAR(500) = NULL,
    @msg NVARCHAR(255) OUT,
    @status VARCHAR(50) OUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @user_id BIGINT = NULL,
        @user_type_id INT,
        @isActive VARCHAR(50),
        @date DATETIME = GETDATE();

    /* =====================================================
       STEP 1 : USER EXIST CHECK (PASSWORD KE BINA)
    ===================================================== */
    SELECT @user_id = tu.user_id, @user_type_id = ISNULL(tu.user_type_id, 2), @isActive = tu.[status] 
    FROM tbl_user tu
    WHERE tu.[user_name] = @username;

    IF @user_id IS NULL
    BEGIN
        SELECT @status = 'failed', @msg = 'User name not found';
        RETURN;
    END

    IF @isActive <> 'Active'
    BEGIN
        SELECT @status = 'failed', @msg = 'User has been deactivated';
        RETURN;
    END

    /* =====================================================
       STEP 2 : PASSWORD CHECK (Task 2.1 PBKDF2 & Plaintext)
    ===================================================== */
    DECLARE @actual_pass NVARCHAR(MAX) = NULL;
    SELECT @actual_pass = tu.[password] FROM tbl_user tu WHERE tu.user_id = @user_id;

    -- If password is not yet hashed with PBKDF2, verify legacy plaintext match
    -- If already hashed with PBKDF2 ($pbkdf2-sha256$...), the C# BAL layer performs constant-time cryptographic verification
    IF (@actual_pass NOT LIKE '$pbkdf2%')
    BEGIN
        IF (@actual_pass <> @password)
        BEGIN
            SELECT @status = 'failed', @msg = 'Invalid password';
            RETURN;
        END
    END

    /* =====================================================
       STEP 3 : LOGIN SUCCESS (EXISTING LOGIC AS-IS)
    ===================================================== */
	IF @user_type_id <> 1
	BEGIN
		SELECT 
			u.user_id, u.user_type_id, u.user_name, v.user_type,u.name, u.phone_no, u.email, u.status,u.password, v.module_url AS url
		   FROM tbl_user u INNER JOIN tbl_user_type v  ON v.user_type_id = u.user_type_id
		  
		   AND u.status = 'Active';
		  SELECT @status = 'success', @msg = '';
		RETURN;
	END
	ELSE
	BEGIN
		SELECT 
			u.user_id, u.user_type_id, u.user_name, v.user_type,u.name, u.phone_no, u.email, u.status,u.password, v.module_url AS url
		    FROM tbl_user u INNER JOIN tbl_user_type v 	ON v.user_type_id = u.user_type_id
			
		    WHERE u.user_id = @user_id;
		  SELECT @status = 'success', @msg = '';
		RETURN;
	END

END

GO

