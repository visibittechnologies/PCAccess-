USE [PCAccess_DB]
GO
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'add_company_details')
DROP PROCEDURE [dbo].[add_company_details]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[add_company_details] 
(
	@flag VARCHAR(150) = NULL, 
	@id INT = NULL,
	@user_id BIGINT = NULL,
	@json_data NVARCHAR(MAX) = NULL,
    @search NVARCHAR(255) = NULL,
    @sortcol NVARCHAR(100) = NULL,
    @sortdir NVARCHAR(20) = NULL,
    @page_no INT = 0,
    @page_size INT = 10
)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @getdate DATETIME = GETUTCDATE();
    DECLARE @Where NVARCHAR(MAX) = '';
    DECLARE @v_Query NVARCHAR(MAX) = '';
	IF (@Flag = 'add')
	BEGIN
		MERGE INTO tbl_company_profile AS target
		USING (
			SELECT 
				JSON_VALUE(@json_data, '$.company_name') company_name,
				JSON_VALUE(@json_data, '$.email') email,
				JSON_VALUE(@json_data, '$.phone_no') phone_no,
				JSON_VALUE(@json_data, '$.whats_app_number') whats_app_number,
				JSON_VALUE(@json_data, '$.address') address,
				JSON_VALUE(@json_data, '$.coprate_address') coprate_address,
				JSON_VALUE(@json_data, '$.title') title,
				JSON_VALUE(@json_data, '$.google_map_url') google_map_url,
				JSON_VALUE(@json_data, '$.facebook_url') facebook_url,
				JSON_VALUE(@json_data, '$.twitter_url') twitter_url,
				JSON_VALUE(@json_data, '$.linkedin_url') linkedin_url,
				JSON_VALUE(@json_data, '$.instagram_url') instagram_url,
				JSON_VALUE(@json_data, '$.youtube_url') youtube_url,
				JSON_VALUE(@json_data, '$.contact_person') contact_person,
				JSON_VALUE(@json_data, '$.secondary_phone') secondary_phone,
				JSON_VALUE(@json_data, '$.fax_no') fax_no,
				JSON_VALUE(@json_data, '$.pinterest_url') pinterest_url,
				JSON_VALUE(@json_data, '$.reddit_url') reddit_url,
				JSON_VALUE(@json_data, '$.tumblr_url') tumblr_url,
				JSON_VALUE(@json_data, '$.meta_keywords') meta_keywords,
				CAST(JSON_VALUE(@json_data, '$.show_in_common') AS BIT) show_in_common,
				CAST(JSON_VALUE(@json_data, '$.enable_public_links') AS BIT) enable_public_links,
				CAST(JSON_VALUE(@json_data, '$.require_manual_review') AS BIT) require_manual_review,
				JSON_VALUE(@json_data, '$.company_logo') company_logo,
				JSON_VALUE(@json_data, '$.privacy_policy') privacy_policy_file,
				JSON_VALUE(@json_data, '$.terms_conditions') terms_conditions_file
		) AS source
		ON target.ID = @id

		WHEN MATCHED THEN
		UPDATE SET
			target.company_name = source.company_name,
			target.email = source.email,
			target.phone_no = source.phone_no,
			target.whats_app_number = source.whats_app_number,
			target.address = source.address,
			target.coprate_address = source.coprate_address,
			target.title = source.title,
			target.google_map_url = source.google_map_url,
			target.facebook_url = source.facebook_url,
			target.twitter_url = source.twitter_url,
			target.linkedin_url = source.linkedin_url,
			target.instagram_url = source.instagram_url,
			target.youtube_url = source.youtube_url,
			target.contact_person = source.contact_person,
			target.secondary_phone = source.secondary_phone,
			target.fax_no = source.fax_no,
			target.pinterest_url = source.pinterest_url,
			target.reddit_url = source.reddit_url,
			target.tumblr_url = source.tumblr_url,
			target.meta_keywords = source.meta_keywords,
			target.show_in_common = source.show_in_common,
			target.enable_public_links = source.enable_public_links,
			target.require_manual_review = source.require_manual_review,
			target.company_logo = source.company_logo,
			target.privacy_policy_file = source.privacy_policy_file,
			target.terms_conditions_file = source.terms_conditions_file,
			target.modified_by = @user_id,
			target.updated_date = @getdate

		WHEN NOT MATCHED THEN
		INSERT (
			user_id, company_name, email, phone_no, whats_app_number,
			address, coprate_address, title,
			google_map_url, facebook_url, twitter_url, linkedin_url, instagram_url, youtube_url,
			contact_person, secondary_phone, fax_no,
			pinterest_url, reddit_url, tumblr_url,
			meta_keywords, show_in_common, enable_public_links, require_manual_review,
			company_logo, privacy_policy_file, terms_conditions_file,
			is_deleted, created_by, created_date
		)
		VALUES (
			@user_id, company_name, email, phone_no, whats_app_number,
			address, coprate_address, title,
			google_map_url, facebook_url, twitter_url, linkedin_url, instagram_url, youtube_url,
			contact_person, secondary_phone, fax_no,
			pinterest_url, reddit_url, tumblr_url,
			meta_keywords, show_in_common, enable_public_links, require_manual_review,
			company_logo, privacy_policy_file, terms_conditions_file,
			0, @user_id, @getdate
		);
		SELECT @id AS id, 'success' AS response;
	END
	IF (@Flag = 'get')
	BEGIN
		SELECT TOP 1 * FROM tbl_company_profile WHERE user_id = @user_id;
	END
	IF (@Flag = 'view')
BEGIN
	SELECT 
		ID,
		user_id,company_name,email,phone_no,whats_app_number,address,coprate_address,title,
		google_map_url,facebook_url,twitter_url,linkedin_url,instagram_url,youtube_url,
		contact_person,secondary_phone,fax_no,pinterest_url,reddit_url,tumblr_url,meta_keywords,show_in_common,enable_public_links,
		require_manual_review,company_logo,
		privacy_policy_file,terms_conditions_file,
		created_by,created_date,modified_by,updated_date
	FROM tbl_company_profile
	WHERE ID = 1;
END
if(@Flag='add_slider')
BEGIN
    DECLARE @maxSequence INT;
    DECLARE @newSequence INT;
	DECLARE @currentSequence INT;
     SELECT @maxSequence = ISNULL(MAX(sequence), 0)
     FROM tbl_slider_master;
	 SELECT @currentSequence = sequence
      FROM tbl_slider_master
      WHERE Id = @id;
    SET @newSequence = JSON_VALUE(@json_data, '$.sequence');
    IF @newSequence IS NULL OR @newSequence < 1
    SET @newSequence = @maxSequence + 1;
    IF EXISTS (SELECT 1 FROM tbl_slider_master WHERE sequence = @newSequence)
    BEGIN

    IF (ISNULL(@currentSequence,0)=0)
		BEGIN
			UPDATE tbl_slider_master
			SET sequence = sequence + 1
			WHERE sequence >= @newSequence;
		END
	ELSE
	BEGIN
    IF (@currentSequence < @newSequence)
    BEGIN
		UPDATE tbl_slider_master
		SET sequence = sequence - 1
		WHERE sequence > @currentSequence AND sequence <= @newSequence;
    END
    ELSE
    BEGIN
        UPDATE tbl_slider_master
        SET sequence = sequence + 1
        WHERE sequence >= @newSequence AND sequence < @currentSequence;
    END
	END
END

MERGE INTO tbl_slider_master  tb_target
		USING (select JSON_VALUE(@json_data, '$.slider_title') slider_title,
				      JSON_VALUE(@json_data, '$.slider_content') slider_content,
			          JSON_VALUE(@json_data, '$.slider_image') slider_image,
					  JSON_VALUE(@json_data, '$.slider_url') slider_url,
					  JSON_VALUE(@json_data, '$.slider_button_title') slider_button_title,
					  JSON_VALUE(@json_data, '$.slider_video_url') slider_video_url,
					  @newSequence AS sequence
					    ) tb_source
		ON tb_target.Id = @id
		WHEN MATCHED THEN UPDATE SET tb_target.slider_title = tb_source.slider_title,
		tb_target.slider_image = tb_source.slider_image,
		tb_target.slider_content = tb_source.slider_content,  
		tb_target.slider_url = tb_source.slider_url,  
	    tb_target.slider_button_title = tb_source.slider_button_title,  
		tb_target.slider_video_url = tb_source.slider_video_url,  
	    tb_target.sequence = tb_source.sequence,
	    tb_target.modified_by = @user_id,
		tb_target.updated_date = @getdate
		WHEN NOT MATCHED BY TARGET THEN 
		INSERT (user_id,slider_title,slider_content,slider_image,slider_url,slider_button_title,slider_video_url,sequence,is_deleted,created_by,created_date)
		VALUES(@user_id,slider_title,slider_content,slider_image,slider_url,slider_button_title,slider_video_url,sequence,0,@user_id,@getdate);
		select @id id,'success' response
END
IF(@Flag='Sliderlist')
BEGIN
    IF ISNULL(@search, '') <> ''
    BEGIN
        SET @Where = @Where + ' AND (ISNULL(CONVERT(NVARCHAR(255), slider_title) + '' '', '''') LIKE ''%' + ISNULL(@search, '') + '%'') '
    END

    SET @v_Query = '
        SELECT Id, slider_title, slider_content, slider_image, slider_url, slider_button_title, slider_video_url, sequence, dbo.func_DateFormate(7, created_date) AS created,
            COUNT(*) OVER() AS overall_count  FROM
            tbl_slider_master  WHERE
            is_deleted = 0 ' + @Where + '
         ORDER BY ' + ISNULL(NULLIF(@sortcol,''), 'Id') + ' ' + ISNULL(NULLIF(@sortdir,''), 'DESC') + '
         OFFSET ' + CONVERT(VARCHAR, ISNULL(@page_no, 0)) + ' ROWS
         FETCH NEXT ' + CONVERT(VARCHAR, ISNULL(@page_size, 10)) + ' ROWS ONLY';

    EXEC (@v_Query);
END

IF(@Flag='getSlider')
BEGIN
    SELECT Id, slider_title,slider_content,slider_image,slider_url,slider_button_title,slider_video_url,sequence,FORMAT(created_date, 'dd-MM-yyyy') AS created 
    FROM tbl_slider_master WHERE Id= @id;
END
if(@Flag='slider')
	begin
		SELECT top 5 Id, slider_title,slider_content,slider_image,slider_url,slider_button_title,slider_video_url,sequence,FORMAT(created_date, 'dd-MM-yyyy') AS created 
		from tbl_slider_master where is_deleted=0  order by sequence
	end
END;

GO

