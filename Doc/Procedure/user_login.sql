USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'user_login')
DROP PROCEDURE [dbo].[user_login]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================
-- PROCEDURE: user_login
-- =========================
CREATE   PROCEDURE [dbo].[user_login]
(
	@username VARCHAR(50),
	@password VARCHAR(100) = NULL,
	@id VARCHAR(20) = NULL,
	@browser VARCHAR(500) = NULL,
	@msg NVARCHAR(255) OUT,
	@status VARCHAR(50) OUT
)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @user_id BIGINT, @user_type_id INT, @isActive VARCHAR(100), @date DATETIME = GETDATE();

	SELECT 
		@user_id = user_id, 
		@user_type_id = user_type_id, 
		@isActive = status
	FROM tbl_user 
	WHERE user_name = @username AND password = @password;

	IF @user_id IS NULL
	BEGIN
		SET @status = 'failed';
		SET @msg = 'User name not found';
		RETURN;
	END

	IF @isActive != 'Active'
	BEGIN
		SET @status = 'failed';
		SET @msg = 'User has been deactivated';
		RETURN;
	END

	SELECT u.user_id,u.user_type_id,u.user_name,v.user_type,u.name,u.phone_no,u.email,u.status,u.password,v.module_url AS url,u.profile_photo
	FROM tbl_user u
	INNER JOIN tbl_user_type v ON v.user_type_id = u.user_type_id
	WHERE u.user_id = @user_id;

	EXEC ua_user_loginfo_iud 
		@user_id = @user_id,
		@login_date = @date,
		@log_type = 1,
		@module_url = 'Login',
		@module_name = 'Log In',
		@qflag = 'I',
		@ip_address = @id,
		@description = @browser;

	SET @status = 'success';
	SET @msg = '';
END;

GO

