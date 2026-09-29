USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_get_dropdown_value')
DROP PROCEDURE [dbo].[proc_get_dropdown_value]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[proc_get_dropdown_value]
	@flag          VARCHAR(MAX),
	@keyId         INT          = 0,
	@ReferenceType VARCHAR(100) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	IF @flag = 'get_brand_dropdown'
	BEGIN

		SELECT 
		BrandId AS MasterId,
		BrandName AS MasterName
		FROM tbl_brand
		WHERE Status = 1 
		AND IsDeleted = 0
		ORDER BY BrandName

	END
	-- Get support needs from references table
	IF @flag = 'get_support_needs'
	BEGIN
		SELECT
			ReferenceId AS MasterId,
			ReferenceName AS MasterName,
			Description AS Description
		FROM m_references
		WHERE Is_deleted = 0 
		  AND ReferenceType = 'SupportType'
		ORDER BY OrderBy
	END
	
	-- Get career positions from references table
	IF @flag = 'get_career_positions'
	BEGIN
		SELECT
			ReferenceId AS MasterId,
			ReferenceName AS MasterName,
			Description AS Description
		FROM m_references
		WHERE Is_deleted = 0 
		  AND ReferenceType = 'CareerPosition'
		ORDER BY OrderBy
	END

	-- Get referral types from references table
	IF @flag = 'get_referral_type'
	BEGIN
		SELECT
			ReferenceId AS MasterId,
			ReferenceName AS MasterName,
			Description AS Description
		FROM m_references
		WHERE Is_deleted = 0 
		  AND ReferenceType = 'ReferralType'
		ORDER BY OrderBy
	END
	
	-- Get contact subjects from references table
	IF @flag = 'get_contact_subject'
	BEGIN
		SELECT
			ReferenceId AS MasterId,
			ReferenceName AS MasterName,
			Description AS Description
		FROM m_references
		WHERE Is_deleted = 0 
		  AND ReferenceType = 'ContactSubject'
		ORDER BY OrderBy
	END
	
	-- Get dropdown values by type
	IF @flag = 'get_dropdown_by_type'
	BEGIN
		SELECT
			ReferenceId   AS MasterId,
			ReferenceName AS MasterName,
			Description   AS Description
		FROM m_references
		WHERE Is_deleted    = 0
		  AND ReferenceType = @ReferenceType
		ORDER BY OrderBy
	END

	-- Get category values from tbl_category by type
	IF @flag = 'get_category_by_type'
	BEGIN
		SELECT
			category_id AS MasterId,
			category_name AS MasterName
		FROM tbl_category
		WHERE is_deleted = 0
		  AND category_type = @ReferenceType
		ORDER BY sequence ASC, category_name ASC
	END
	-- Get distinct category types for the blog_type dropdown
	IF @flag = 'get_distinct_category_type'
	BEGIN
		SELECT DISTINCT
			category_type AS MasterId,
			category_type AS MasterName
		FROM tbl_category
		WHERE is_deleted = 0
		ORDER BY category_type ASC
	END

END

GO

