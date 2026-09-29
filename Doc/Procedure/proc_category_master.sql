USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_category_master')
DROP PROCEDURE [dbo].[proc_category_master]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[proc_category_master]
	@flag          VARCHAR(120)    = NULL,
	@id            INT             = NULL,
	@user_id       BIGINT          = NULL,
	@json_data     NVARCHAR(MAX)   = NULL,
	@search        NVARCHAR(MAX)   = NULL,
	@page_no       INT             = 1,
	@page_size     INT             = 30,
	@sortcol       VARCHAR(150)    = 'category_id',
	@sortdir       VARCHAR(5)      = 'desc',
	@category_type VARCHAR(100)    = NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @v_Query  AS VARCHAR(MAX) = '',
	        @Where    AS VARCHAR(MAX) = ''

	IF (@flag = 'add_category')
	BEGIN
		-- Extract values from JSON
		DECLARE @CategoryName NVARCHAR(450),
		        @CategoryType NVARCHAR(450),
		        @Sequence INT

		SET @CategoryName = JSON_VALUE(@json_data, '$.category_name')
		SET @CategoryType = JSON_VALUE(@json_data, '$.category_type')
		SET @Sequence = TRY_CAST(JSON_VALUE(@json_data, '$.sequence') AS INT)

		IF @Sequence IS NULL SET @Sequence = 1

		IF (@id > 0)
		BEGIN
			-- Update
			UPDATE [dbo].[tbl_category]
			SET category_name = @CategoryName,
			    category_type = @CategoryType,
			    sequence = @Sequence,
			    modified_by = @user_id,
			    updated_date = GETDATE()
			WHERE category_id = @id;

			SELECT @id AS id, 'success' AS response;
		END
		ELSE
		BEGIN
			-- Insert
			INSERT INTO [dbo].[tbl_category] (category_name, category_type, sequence, is_deleted, created_by, created_date)
			VALUES (@CategoryName, @CategoryType, @Sequence, 0, @user_id, GETDATE());

			DECLARE @NewId BIGINT = SCOPE_IDENTITY();
			SELECT @NewId AS id, 'success' AS response;
		END
	END

	IF (@flag = 'list')
	BEGIN
		IF ISNULL(@search, '') <> ''
		BEGIN
			SET @Where = @Where + 'AND (ISNULL(category_name, '''') LIKE ''%' + ISNULL(@search, '') + '%'') '
		END

		SET @v_Query = '
			SELECT
				category_id AS Id,
				category_name AS CategoryName,
				category_type AS CategoryType,
				sequence AS Sequence,
				FORMAT(created_date, ''MMM dd, yyyy'') AS CreatedDate,
				COUNT(*) OVER() AS overall_count
			FROM [dbo].[tbl_category]
			WHERE is_deleted = 0 ' + @Where + '
			ORDER BY ' + @sortcol + ' ' + @sortdir + '
			OFFSET '  + CONVERT(VARCHAR, (@page_no - 1) * @page_size)   + ' ROWS
			FETCH NEXT ' + CONVERT(VARCHAR, @page_size) + ' ROWS ONLY'

		EXEC (@v_Query)
	END

	IF (@flag = 'get')
	BEGIN
		SELECT
			category_id AS Id,
			category_name AS CategoryName,
			category_type AS CategoryType,
			sequence AS Sequence
		FROM [dbo].[tbl_category]
		WHERE category_id = @id;
	END

	IF (@flag = 'get_dropdown_by_type')
	BEGIN
		SELECT
			category_id AS MasterId,
			category_name AS MasterName
		FROM [dbo].[tbl_category]
		WHERE is_deleted = 0 AND category_type = @category_type
		ORDER BY sequence ASC, category_name ASC;
	END
END

GO

