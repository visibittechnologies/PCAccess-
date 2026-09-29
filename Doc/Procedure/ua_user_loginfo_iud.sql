USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'ua_user_loginfo_iud')
DROP PROCEDURE [dbo].[ua_user_loginfo_iud]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[ua_user_loginfo_iud]
(
	@user_loginfo_id NUMERIC(18,0)= NULL,
	@user_id BIGINT= NULL,
	@ip_address VARCHAR(20) = NULL,
	@login_date DATETIME = NULL,
	@login_token VARCHAR(50) = NULL,
	@log_type INT = NULL,
	@module_url VARCHAR(250) = NULL,
	@module_name VARCHAR(250) = NULL,
	@description VARCHAR(500) = NULL,
	@qflag CHAR(1)
)
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRAN
	BEGIN TRY

		IF(@qflag='I')
		BEGIN
			SELECT @user_loginfo_id = ISNULL(MAX(user_loginfo_id),0)+1 
			FROM tbl_user_loginfo;

			INSERT INTO tbl_user_loginfo
			(user_loginfo_id,user_id,ip_address,login_date,login_token,log_type,module_url,module_name,description)
			VALUES
			(@user_loginfo_id,@user_id,@ip_address,@login_date,@login_token,@log_type,@module_url,@module_name,@description);
		END

	END TRY
	BEGIN CATCH
		ROLLBACK TRAN;
		THROW;
	END CATCH

	COMMIT TRAN;
END;

GO

