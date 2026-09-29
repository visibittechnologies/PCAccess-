USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_add_get_common_data')
DROP PROCEDURE [dbo].[proc_add_get_common_data]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[proc_add_get_common_data] 
@flag VARCHAR(50),
@add_update_jsonData nvarchar(max) = null,
@PageSize int=null,
@PageNo int = null,
@SearchCriteria nvarchar(max) = null,
@filter_list nvarchar(max) = null,
@msg nvarchar(255) out,
@status VARCHAR(200) out
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @v_Query AS VARCHAR(MAX) = '', @Where AS VARCHAR(MAX) = '';

	IF(@flag='category_list')
	BEGIN
		SET @Where='';
		IF (ISNULL(@SearchCriteria,'') NOT IN ('','[]'))
		BEGIN
			SET @Where = @Where + ' AND c.category_name LIKE ''%' + ISNULL(@SearchCriteria,'') + '%'' ' 
		END

		SELECT @status = 'success', @msg = 'success'
		SET @v_Query= '
			SELECT COUNT(c.category_id) OVER() overall_count,
			       ROW_NUMBER() OVER(ORDER BY c.category_id DESC) AS SN,
			       c.category_id AS Id, 
			       c.category_name AS CategoryName, 
			       c.category_type AS CategoryType,
				   c.sequence AS Sequence,
				   FORMAT(c.created_date, ''MMM dd, yyyy'') AS CreatedDate
			FROM [dbo].[tbl_category] c
			WHERE c.is_deleted = 0' + @Where + ' 
			ORDER BY SN 
			OFFSET ' + CONVERT(varchar,(@PageNo-1)*@PageSize) + ' ROWS 
			FETCH NEXT ' + CONVERT(varchar,@PageSize) + ' rows only'
		EXECUTE(@v_Query)
	END
END

GO

