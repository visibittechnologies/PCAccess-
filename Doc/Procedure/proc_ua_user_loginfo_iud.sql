USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_ua_user_loginfo_iud')
DROP PROCEDURE [dbo].[proc_ua_user_loginfo_iud]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:  <->
-- Create date: <30-Oct-2020>
-- Description: <Insert,Update and Delete data for Front Office Visitor List>
-- =============================================
CREATE PROCEDURE [dbo].[proc_ua_user_loginfo_iud]
(
	@user_loginfo_id numeric(18,0)= null,	 -- Auto Incrimental Number 
	@user_id bigint= null,
	@ip_address varchar(20) = null, 
	@login_date datetime = null,
	@login_token varchar(50) = null,
	@log_type int = null,
	@module_url varchar(250) = null,
	@module_name varchar(250) = null,
	@description varchar(500) = null,
	@qflag char(1),
	@browser nvarchar(max) = null,
	@city nvarchar(500) = null,
	@country nvarchar(500) = null
)
AS
BEGIN
BEGIN TRAN
	BEGIN TRY
		IF(@qflag='I')
		BEGIN
			select @user_loginfo_id = (isnull(max(user_loginfo_id),0)+1) from tbl_user_loginfo
			IF(@user_loginfo_id>0)
			BEGIN
				INSERT INTO tbl_user_loginfo(user_loginfo_id,[user_id],ip_address,login_date,[login_token],log_type,module_url,module_name,description,browser,city,country)
				Values(@user_loginfo_id,@user_id,@ip_address,@login_date,@login_token,@log_type,@module_url,@module_name,@description,@browser,@city,@country)

				update tbl_user set LastLoginDate = getdate(), LastIpAddress = @ip_address where user_id = @user_id and @log_type = 1;
				update tbl_user set LastActivityDate = getdate() where user_id = @user_id;
			END
		END
	END TRY
	BEGIN CATCH
		ROLLBACK TRAN
		retuRN ERROR_MESSAGE();			
	END CATCH
COMMIT TRAN
return @user_loginfo_id;
END


GO

