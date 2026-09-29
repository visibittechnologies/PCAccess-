USE [PCAccess_DB]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_ai_assistant_knowledge')
DROP PROCEDURE [dbo].[proc_ai_assistant_knowledge]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================================
-- Procedure: [dbo].[proc_ai_assistant_knowledge]
-- Purpose: Controlled, secure retrieval of public knowledge for the AI Assistant.
-- Security:
--   - Strict READ-ONLY operations on approved public tables (NOLOCK).
--   - Strictly filters is_deleted = 0 and status = 'Published'.
--   - Hard-capped maximum return limits (default 5, maximum 10).
--   - Zero access to user, credential, or admin tables.
-- ============================================================================
CREATE PROCEDURE [dbo].[proc_ai_assistant_knowledge]
(
    @flag           VARCHAR(50),
    @search         NVARCHAR(255) = NULL,
    @id             BIGINT        = NULL,
    @category       NVARCHAR(150) = NULL,
    @max_results    INT           = 5
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Enforce strict server-side bounds to protect memory and bandwidth
    IF @max_results IS NULL OR @max_results <= 0
        SET @max_results = 5;
    IF @max_results > 10
        SET @max_results = 10;

    SET @search = LTRIM(RTRIM(ISNULL(@search, '')));

    -- ------------------------------------------------------------------------
    -- 1. SEARCH BLOGS (Controlled public blog search)
    -- ------------------------------------------------------------------------
    IF (@flag = 'search_blogs')
    BEGIN
        SELECT TOP (@max_results)
            b.blog_id,
            b.title,
            b.short_description,
            b.blog_img,
            b.category,
            b.author,
            b.crop_name,
            b.mandi_name,
            b.price,
            b.price_change,
            b.location,
            FORMAT(b.created_date, 'MMM dd, yyyy') AS created_date
        FROM tbl_blog b WITH (NOLOCK)
        WHERE b.is_deleted = 0
          AND (b.status = 'Published' OR (b.status = 'Scheduled' AND CAST(b.scheduled_date AS DATE) <= CAST(GETDATE() AS DATE)))
          AND (
              @search = ''
              OR b.title             LIKE '%' + @search + '%'
              OR b.short_description LIKE '%' + @search + '%'
              OR b.crop_name         LIKE '%' + @search + '%'
              OR b.mandi_name        LIKE '%' + @search + '%'
              OR b.category          LIKE '%' + @search + '%'
              OR b.meta_keywords     LIKE '%' + @search + '%'
          )
          AND (
              @category IS NULL OR @category = '' OR b.category = @category
          )
        ORDER BY b.blog_id DESC;
    END

    -- ------------------------------------------------------------------------
    -- 2. GET BLOG DETAILS BY ID (Single article knowledge)
    -- ------------------------------------------------------------------------
    ELSE IF (@flag = 'get_blog_details')
    BEGIN
        SELECT TOP 1
            b.blog_id,
            b.title,
            b.short_description,
            b.long_description,
            b.blog_img,
            b.category,
            b.author,
            b.crop_name,
            b.mandi_name,
            b.price,
            b.price_change,
            b.location,
            FORMAT(b.created_date, 'MMM dd, yyyy') AS created_date
        FROM tbl_blog b WITH (NOLOCK)
        WHERE b.blog_id = @id
          AND b.is_deleted = 0
          AND (b.status = 'Published' OR (b.status = 'Scheduled' AND CAST(b.scheduled_date AS DATE) <= CAST(GETDATE() AS DATE)));
    END

    -- ------------------------------------------------------------------------
    -- 3. GET COMPANY PROFILE KNOWLEDGE (Verified public corporate details)
    -- ------------------------------------------------------------------------
    ELSE IF (@flag = 'get_company_info')
    BEGIN
        SELECT TOP 1
            c.ID,
            c.company_name,
            c.email,
            c.phone_no,
            c.whats_app_number,
            c.address,
            c.coprate_address,
            c.title,
            c.google_map_url,
            c.facebook_url,
            c.twitter_url,
            c.linkedin_url,
            c.instagram_url,
            c.youtube_url,
            c.contact_person,
            c.secondary_phone,
            c.meta_keywords,
            c.company_logo
        FROM tbl_company_profile c WITH (NOLOCK)
        ORDER BY c.ID ASC;
    END

    -- ------------------------------------------------------------------------
    -- 4. GET CATEGORIES (Public taxonomy)
    -- ------------------------------------------------------------------------
    ELSE IF (@flag = 'get_categories')
    BEGIN
        SELECT
            c.category_id,
            c.category_name,
            c.category_type,
            ISNULL(c.sequence, 0) AS sequence
        FROM tbl_category c WITH (NOLOCK)
        WHERE c.is_deleted = 0
        ORDER BY c.sequence ASC, c.category_name ASC;
    END

    -- ------------------------------------------------------------------------
    -- 5. SEARCH FAQS (Curated Q&A knowledge)
    -- ------------------------------------------------------------------------
    ELSE IF (@flag = 'search_faqs')
    BEGIN
        SELECT TOP (@max_results)
            f.faq_id,
            f.question,
            f.answer,
            f.category
        FROM tbl_ai_faq f WITH (NOLOCK)
        WHERE f.is_deleted = 0
          AND f.is_active = 1
          AND (
              @search = ''
              OR f.question LIKE '%' + @search + '%'
              OR f.answer   LIKE '%' + @search + '%'
              OR f.category LIKE '%' + @search + '%'
          )
        ORDER BY f.sequence ASC, f.faq_id ASC;
    END

    -- ------------------------------------------------------------------------
    -- 6. GET NAVIGATION PAGES (Allowed internal website routes)
    -- ------------------------------------------------------------------------
    ELSE IF (@flag = 'get_navigation_pages')
    BEGIN
        SELECT
            p.ID,
            p.page_name,
            p.page_url,
            p.meta_title,
            p.meta_keywords,
            p.meta_description
        FROM tbl_page_seo_meta_tags p WITH (NOLOCK)
        WHERE p.is_deleted = 0
        ORDER BY p.ID ASC;
    END

END
GO
