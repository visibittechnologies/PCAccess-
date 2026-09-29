USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_project_remove')
DROP PROCEDURE [dbo].[proc_project_remove]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[proc_project_remove]
(
	@Flag NVARCHAR(50),
	@Id INT
)
AS
BEGIN
	SET NOCOUNT ON;
	IF(@Flag = 'delete_slider')
	BEGIN
		UPDATE tbl_slider_master 
		SET is_deleted = 1 
		WHERE Id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_career_request')
	BEGIN
		UPDATE tbl_career_request 
		SET is_deleted = 1 
		WHERE request_id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_career_master')
	BEGIN
		UPDATE tbl_career_master 
		SET is_deleted = 1 
		WHERE job_id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_contact_request')
	BEGIN
		UPDATE tbl_contact_request 
		SET IsDeleted = 1 
		WHERE Id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'delete_testimonial')
	BEGIN
		UPDATE tbl_testimonial 
		SET is_deleted = 1 
		WHERE ID = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_page_meta')
	BEGIN
		UPDATE tbl_page_seo_meta_tags 
		SET is_deleted = 1 
		WHERE ID = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_google_script')
	BEGIN
		UPDATE tbl_google_script 
		SET is_deleted = 1 
		WHERE ID = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_referral_request')
	BEGIN
		UPDATE tbl_referral_request 
		SET IsDeleted = 1 
		WHERE Id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_blog')
	BEGIN
		UPDATE tbl_blog 
		SET is_deleted = 1 
		WHERE blog_id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_profile_image')
	BEGIN
		UPDATE tbl_user
		SET profile_photo = NULL 
		WHERE user_id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_assessment_request')
	BEGIN
		UPDATE tbl_assessment_request 
		SET IsDeleted = 1 
		WHERE Id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_gallery')
	BEGIN
		UPDATE tbl_gallery 
		SET is_deleted = 1 
		WHERE Id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
	ELSE IF(@Flag = 'remove_category')
	BEGIN
		UPDATE tbl_category 
		SET is_deleted = 1 
		WHERE category_id = @Id;
		SELECT @Id AS id, 'success' AS response;
	END
END
GO

