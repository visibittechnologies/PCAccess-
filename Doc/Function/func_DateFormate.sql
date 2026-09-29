USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type IN ('FN', 'IF', 'TF', 'FS', 'FT') AND name = 'func_DateFormate')
DROP FUNCTION [dbo].[func_DateFormate]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[func_DateFormate]
(
    @flag INT,
    @datetime DATETIME
)
RETURNS NVARCHAR(50)
AS
BEGIN

    -- FLAG 1 : dd/MM/yyyy
    IF (@flag = 1)
        RETURN CAST(FORMAT(@datetime, 'dd/MM/yyyy') AS NVARCHAR(50));

    -- FLAG 2 : dd/MM/yyyy, hh:mm tt
    ELSE IF (@flag = 2)
        RETURN CAST(FORMAT(@datetime, 'dd/MM/yyyy, hh:mm tt') AS NVARCHAR(50));

    -- FLAG 3 : hh:mm tt
    ELSE IF (@flag = 3)
        RETURN CAST(FORMAT(@datetime, 'hh:mm tt') AS NVARCHAR(50));

    -- FLAG 4 : MM/dd/yyyy
    ELSE IF (@flag = 4)
        RETURN CAST(FORMAT(@datetime, 'MM/dd/yyyy') AS NVARCHAR(50));

    -- FLAG 5 : yyyy-MM-dd
    ELSE IF (@flag = 5)
        RETURN CAST(FORMAT(@datetime, 'yyyy-MM-dd') AS NVARCHAR(50));

    -- FLAG 6 : UTC ? EST (DST handled)
    ELSE IF (@flag = 6)
    BEGIN
        DECLARE @est_datetime DATETIME;
        SET @est_datetime =
            CAST(
                @datetime AT TIME ZONE 'UTC'
                AT TIME ZONE 'Eastern Standard Time'
                AS DATETIME
            );

        RETURN CAST(@est_datetime AS NVARCHAR(50));
    END

    -- FLAG 7 : MMM dd, yyyy, hh:mm tt
ELSE IF (@flag = 7)
    RETURN CAST(FORMAT(@datetime, 'MMM dd, yyyy') AS NVARCHAR(50));

    -- DEFAULT
    RETURN CAST(FORMAT(@datetime, 'dd/MM/yyyy') AS NVARCHAR(50));

END

GO

