USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'update_blog_view')
DROP PROCEDURE [dbo].[update_blog_view]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE update_blog_view
    @blog_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tbl_blog 
    SET view_count = ISNULL(view_count, 0) + 1 
    WHERE blog_id = @blog_id;

    SELECT view_count FROM tbl_blog WHERE blog_id = @blog_id;
END

GO

