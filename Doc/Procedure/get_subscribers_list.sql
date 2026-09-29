USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'get_subscribers_list')
DROP PROCEDURE [dbo].[get_subscribers_list]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE get_subscribers_list
    @lValue1 BIGINT = 0, -- Used for flag (e.g. 1 for active)
    @lValue2 BIGINT = 0, -- Could be specific ID
    @iDisplayStart INT = 0,
    @iDisplayLength INT = 10,
    @sSearch NVARCHAR(250) = '',
    @sSortColName NVARCHAR(100) = 'Id',
    @sSortDir_0 NVARCHAR(10) = 'desc'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TotalCount INT;
    
    SELECT @TotalCount = COUNT(Id)
    FROM tbl_subscribers
    WHERE (@sSearch = '' OR Email LIKE '%' + @sSearch + '%');
    
    -- Using ROW_NUMBER for pagination
    SELECT *, @TotalCount AS overall_count
    FROM (
        SELECT 
            Id,
            Email,
            IsActive,
            IsVerified,
            CONVERT(VARCHAR(20), SubscribeDate, 106) AS SubscribeDateFormatted,
            CONVERT(VARCHAR(20), UnsubscribeDate, 106) AS UnsubscribeDateFormatted,
            IPAddress,
            SubscribeDate,
            ROW_NUMBER() OVER (
                ORDER BY 
                    CASE WHEN @sSortColName = 'Id' AND @sSortDir_0 = 'asc' THEN Id END ASC,
                    CASE WHEN @sSortColName = 'Id' AND @sSortDir_0 = 'desc' THEN Id END DESC,
                    CASE WHEN @sSortColName = 'Email' AND @sSortDir_0 = 'asc' THEN Email END ASC,
                    CASE WHEN @sSortColName = 'Email' AND @sSortDir_0 = 'desc' THEN Email END DESC,
                    CASE WHEN @sSortColName = 'SubscribeDate' AND @sSortDir_0 = 'asc' THEN SubscribeDate END ASC,
                    CASE WHEN @sSortColName = 'SubscribeDate' AND @sSortDir_0 = 'desc' THEN SubscribeDate END DESC
            ) AS RowNum
        FROM tbl_subscribers
        WHERE (@sSearch = '' OR Email LIKE '%' + @sSearch + '%')
    ) AS Result
    WHERE RowNum > @iDisplayStart AND RowNum <= (@iDisplayStart + @iDisplayLength);
END

GO

