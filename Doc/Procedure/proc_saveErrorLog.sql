USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_saveErrorLog')
DROP PROCEDURE [dbo].[proc_saveErrorLog]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[proc_saveErrorLog]
    @flag INT ,
    @ExceptionMessage NVARCHAR(MAX) ,
    @ControllerName NVARCHAR(MAX) ,
    @ExceptionStackTrace NVARCHAR(MAX),
	@modelJson NVARCHAR(MAX)
AS
    BEGIN
        IF ( @flag = 1 )
            BEGIN
                INSERT  INTO dbo.ExceptionLogger
                        ( ExceptionMessage ,
                          ControllerName ,
                          ExceptionStackTrace ,
						  modelJson,
                          LogTime
		                )
                VALUES  ( @ExceptionMessage , -- ExceptionMessage - nvarchar(max)
                          @ControllerName , -- ControllerName - nvarchar(50)
                          @ExceptionStackTrace , -- ExceptionStackTrace - nvarchar(max)
						  @modelJson,
                          GETDATE()  -- LogTime - datetime
		                );
            END;

        ELSE
            IF ( @flag = 2 )
                BEGIN
                    SELECT  E.Id ,
                            E.ExceptionMessage ,
                            E.ControllerName ,
                            E.ExceptionStackTrace ,
                            E.LogTime
                    FROM    ExceptionLogger E
                    ORDER BY Id DESC;
                END;
    END;

GO

