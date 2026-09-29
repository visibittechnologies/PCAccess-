USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'testimonial_settings')
DROP PROCEDURE [dbo].[testimonial_settings]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
---------------------- CREATE SETTINGS PROC ----------------------
CREATE PROCEDURE [dbo].[testimonial_settings]
(
    @flag NVARCHAR(150),
    @value BIGINT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    IF(@flag='is_testimonial_show')
    BEGIN
        UPDATE tbl_website_settings  
        SET SettingValue = @value  
        WHERE SettingKey = 'ShowAllTestimonialsOnHome';

        SELECT 'success' AS response;
    END
    IF(@flag='get_testimonial_toggle')
    BEGIN
        SELECT ISNULL(SettingValue, 0) AS SettingValue
        FROM tbl_website_settings
        WHERE SettingKey = 'ShowAllTestimonialsOnHome';
    END
END

GO

