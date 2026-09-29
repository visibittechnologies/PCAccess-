USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_get_admin_dashboard_stats')
DROP PROCEDURE [dbo].[sp_get_admin_dashboard_stats]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE sp_get_admin_dashboard_stats
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 1. KPI Counts
    SELECT 
        (SELECT COUNT(1) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0) AS TotalBlogs,
        (SELECT COUNT(1) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0 AND status = 'Published') AS PublishedBlogs,
        (SELECT COUNT(1) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0 AND status = 'Draft') AS DraftBlogs,
        (SELECT COUNT(1) FROM tbl_category WHERE ISNULL(is_deleted, 0) = 0) AS TotalCategories,
        (SELECT COUNT(1) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0 AND (blog_type = 'Video' OR blog_type = 'Social Feed')) AS TotalVideos,
        (SELECT COUNT(1) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0 AND (blog_type = 'Reel' OR blog_type = 'Agri-Reels')) AS TotalReels,
        (SELECT COUNT(1) FROM tbl_subscribers WHERE IsActive = 1) AS TotalSubscribers,
        0 AS TotalComments;
        
    -- 2. Recent Content List (Top 5 Viewed)
    SELECT TOP 5
        blog_id,
        title,
        category,
        author,
        status,
        created_date,
        (ISNULL(view_count, 0) + dbo.ParseViewCount(short_description)) AS view_count,
        blog_type
    FROM tbl_blog
    WHERE ISNULL(is_deleted, 0) = 0
    ORDER BY (ISNULL(view_count, 0) + dbo.ParseViewCount(short_description)) DESC, created_date DESC;

    -- 3. Monthly Blog Views (Current Year, Backfilled + Real-time via Tracking Table)
    DECLARE @CurrentYear INT = YEAR(GETDATE());
    DECLARE @StartDate DATE = DATEFROMPARTS(@CurrentYear, 1, 1);
    
    WITH Months AS (
        SELECT @StartDate AS MonthDate
        UNION ALL
        SELECT DATEADD(MONTH, 1, MonthDate)
        FROM Months
        WHERE MONTH(MonthDate) < 12
    )
    SELECT 
        LEFT(DATENAME(MONTH, m.MonthDate), 3) AS MonthName,
        ISNULL(SUM(v.views_count), 0) AS ViewCount
    FROM Months m
    LEFT JOIN tbl_blog_views_tracking v ON MONTH(v.view_date) = MONTH(m.MonthDate) 
                                       AND YEAR(v.view_date) = YEAR(m.MonthDate)
    GROUP BY m.MonthDate
    ORDER BY m.MonthDate;

    -- 4. Traffic Split (Direct vs Social) using the exact same combined logic so the total matches exactly
    DECLARE @TotalVisitors BIGINT = (SELECT SUM(dbo.ParseViewCount(short_description) + ISNULL(view_count, 0)) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0);
    DECLARE @DirectTraffic BIGINT = (SELECT SUM(dbo.ParseViewCount(short_description) + ISNULL(view_count, 0)) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0 AND ISNULL(blog_type, '') NOT IN ('Social Feed', 'Agri-Reels', 'Video', 'Reel'));
    DECLARE @SocialTraffic BIGINT = (SELECT SUM(dbo.ParseViewCount(short_description) + ISNULL(view_count, 0)) FROM tbl_blog WHERE ISNULL(is_deleted, 0) = 0 AND ISNULL(blog_type, '') IN ('Social Feed', 'Agri-Reels', 'Video', 'Reel'));
    
    SELECT 
        ISNULL(@TotalVisitors, 0) AS TotalVisitors,
        ISNULL(@DirectTraffic, 0) AS DirectTraffic,
        ISNULL(@SocialTraffic, 0) AS SocialTraffic;

END

GO

