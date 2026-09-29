USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_add_page_meta_tags')
DROP PROCEDURE [dbo].[proc_add_page_meta_tags]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[proc_add_page_meta_tags]
    @flag NVARCHAR(100),
	@id INT = NULL OUTPUT, 
	@user_id BIGINT = 0, 
	@page_name NVARCHAR(350),
	@page_url NVARCHAR(MAX),
	@meta_title NVARCHAR(MAX),
	@meta_keywords NVARCHAR(MAX),
	@meta_description NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @flag = 'insert'
    BEGIN
        INSERT INTO tbl_page_seo_meta_tags (page_name,page_url,meta_title,meta_keywords,meta_description, is_deleted, created_by, created_date)
        VALUES (@page_name,@page_url,@meta_title,@meta_keywords,@meta_description,0, @user_id, GETUTCDATE());

        SET @id = SCOPE_IDENTITY();

        SELECT @id AS Id, page_name,page_url FROM tbl_page_seo_meta_tags WHERE ID = @id;
    END
    ELSE IF @flag = 'update'
    BEGIN
        UPDATE tbl_page_seo_meta_tags 
        SET page_name = @page_name,page_url=@page_url,meta_title=@meta_title,meta_keywords=@meta_keywords,meta_description=@meta_description,
            modified_by = @user_id,
            updated_date = GETUTCDATE()
        WHERE Id = @id;

        SELECT @id AS Id, page_name,page_url FROM tbl_page_seo_meta_tags WHERE ID = @id;
    END
    ELSE IF @flag = 'view'
    BEGIN
       SELECT ID, page_name,page_url,meta_title,meta_keywords,meta_description FROM tbl_page_seo_meta_tags WHERE is_deleted=0 ORDER BY Id DESC;
    END
    ELSE IF @flag = 'get'
    BEGIN
       SELECT ID, page_name,page_url,meta_title,meta_keywords,meta_description FROM tbl_page_seo_meta_tags WHERE is_deleted=0 AND Id=@id;
    END
	ELSE IF @flag = 'getmeta_data'
	BEGIN
		DECLARE @normalizedUrl NVARCHAR(500)
    
		SET @normalizedUrl = RIGHT(@page_url, CHARINDEX('/', REVERSE(@page_url))-1)

		SELECT TOP 1 ID, page_name, page_url, meta_title, meta_keywords, meta_description 
		FROM tbl_page_seo_meta_tags 
		WHERE is_deleted = 0 
		AND page_url LIKE '%' + @normalizedUrl
		ORDER BY ID DESC;
	END
    ELSE IF @flag = 'remove'
    BEGIN
        UPDATE tbl_page_seo_meta_tags SET is_deleted=1 WHERE ID = @id;
	    SELECT @id AS id, 'success' AS response;
    END
END

GO

