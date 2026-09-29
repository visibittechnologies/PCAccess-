USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type IN ('FN', 'IF', 'TF', 'FS', 'FT') AND name = 'ParseViewCount')
DROP FUNCTION [dbo].[ParseViewCount]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION dbo.ParseViewCount(@input NVARCHAR(MAX))
RETURNS BIGINT
AS
BEGIN
    IF @input IS NULL RETURN 0;
    
    DECLARE @pos INT = PATINDEX('%[0-9]%', @input);
    IF @pos = 0 RETURN 0;
    
    DECLARE @numStr NVARCHAR(50) = '';
    DECLARE @len INT = LEN(@input);
    DECLARE @i INT = @pos;
    DECLARE @c CHAR(1);
    
    WHILE @i <= @len
    BEGIN
        SET @c = SUBSTRING(@input, @i, 1);
        IF @c LIKE '[0-9]' OR @c = '.'
            SET @numStr = @numStr + @c;
        ELSE
            BREAK;
        SET @i = @i + 1;
    END
    
    DECLARE @multiplier BIGINT = 1;
    IF @i <= @len
    BEGIN
        SET @c = UPPER(SUBSTRING(@input, @i, 1));
        IF @c = 'K' SET @multiplier = 1000;
        ELSE IF @c = 'M' SET @multiplier = 1000000;
    END

    DECLARE @res FLOAT = TRY_CAST(@numStr AS FLOAT);
    IF @res IS NOT NULL RETURN CAST((@res * @multiplier) AS BIGINT);
    
    RETURN 0;
END

GO

