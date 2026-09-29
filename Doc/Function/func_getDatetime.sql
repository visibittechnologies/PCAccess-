USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type IN ('FN', 'IF', 'TF', 'FS', 'FT') AND name = 'func_getDatetime')
DROP FUNCTION [dbo].[func_getDatetime]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[func_getDatetime]() --SELECT  dbo.func_getDatetime()
RETURNS DATETIME
AS BEGIN
--RETURN  CAST(SWITCHOFFSET(SYSDATETIMEOFFSET(), '+05:30') AS datetime)  --Indian
RETURN CAST(SWITCHOFFSET(SYSDATETIMEOFFSET(), '+00:00') AS datetime) --UK
END





GO

