USE [PCAccess_DB]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'proc_ai_assistant_conversation')
DROP PROCEDURE [dbo].[proc_ai_assistant_conversation]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================================
-- Procedure: [dbo].[proc_ai_assistant_conversation]
-- Purpose: Secure, server-authoritative conversation session & context management.
-- Security:
--   - Strict ownership validation (anti-IDOR): caller must match either user_id or anonymous_session_id.
--   - Parameterized statements, bounded result sets.
-- ============================================================================
CREATE PROCEDURE [dbo].[proc_ai_assistant_conversation]
(
    @flag                   VARCHAR(50),
    @conversation_id        NVARCHAR(100),
    @user_id                BIGINT        = NULL,
    @anonymous_session_id   NVARCHAR(100) = NULL,
    @title                  NVARCHAR(250) = NULL,
    @summary                NVARCHAR(MAX) = NULL,
    @role                   VARCHAR(20)   = NULL,
    @message_text           NVARCHAR(MAX) = NULL,
    @message_type           VARCHAR(50)   = 'text',
    @client_message_id      NVARCHAR(100) = NULL,
    @structured_context_json NVARCHAR(MAX) = NULL,
    @max_messages           INT           = 10,
    @pref_category          VARCHAR(50)   = NULL,
    @pref_key               NVARCHAR(100) = NULL,
    @pref_value             NVARCHAR(250) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- ------------------------------------------------------------------------
    -- 1. CREATE OR VALIDATE CONVERSATION (Anti-IDOR Check)
    -- ------------------------------------------------------------------------
    IF (@flag = 'create_or_validate')
    BEGIN
        IF EXISTS (SELECT 1 FROM tbl_ai_conversation WHERE conversation_id = @conversation_id)
        BEGIN
            -- Conversation exists: Validate ownership
            DECLARE @owner_user_id BIGINT;
            DECLARE @owner_anon_id NVARCHAR(100);

            SELECT 
                @owner_user_id = user_id, 
                @owner_anon_id = anonymous_session_id
            FROM tbl_ai_conversation 
            WHERE conversation_id = @conversation_id;

            -- Check ownership match
            IF (@user_id IS NOT NULL AND @owner_user_id IS NOT NULL AND @owner_user_id = @user_id)
               OR (@user_id IS NULL AND @anonymous_session_id IS NOT NULL AND @owner_anon_id = @anonymous_session_id)
               OR (@user_id IS NOT NULL AND @owner_user_id IS NULL AND @anonymous_session_id IS NOT NULL AND @owner_anon_id = @anonymous_session_id)
            BEGIN
                -- Valid owner: update last activity
                UPDATE tbl_ai_conversation
                SET last_activity_date = GETDATE(),
                    user_id = ISNULL(user_id, @user_id)
                WHERE conversation_id = @conversation_id;

                SELECT 1 AS is_authorized, 'Authorized' AS message, conversation_id, title, summary, status
                FROM tbl_ai_conversation 
                WHERE conversation_id = @conversation_id;
            END
            ELSE
            BEGIN
                -- Unauthorized access attempt
                SELECT 0 AS is_authorized, 'Access Denied: Conversation does not belong to this identity' AS message;
            END
        END
        ELSE
        BEGIN
            -- New conversation: Bind ownership securely
            INSERT INTO tbl_ai_conversation 
            (
                conversation_id, user_id, anonymous_session_id, title, status, is_active, is_deleted, created_date, last_activity_date
            )
            VALUES 
            (
                @conversation_id, @user_id, @anonymous_session_id, ISNULL(@title, 'New Conversation'), 'Active', 1, 0, GETDATE(), GETDATE()
            );

            SELECT 1 AS is_authorized, 'Created' AS message, @conversation_id AS conversation_id, ISNULL(@title, 'New Conversation') AS title, NULL AS summary, 'Active' AS status;
        END
        RETURN;
    END

    -- ------------------------------------------------------------------------
    -- 2. GET CONVERSATION DETAILS (With Ownership Check)
    -- ------------------------------------------------------------------------
    IF (@flag = 'get_conversation')
    BEGIN
        SELECT TOP 1
            conversation_id,
            user_id,
            anonymous_session_id,
            title,
            summary,
            status,
            created_date,
            last_activity_date
        FROM tbl_ai_conversation WITH (NOLOCK)
        WHERE conversation_id = @conversation_id
          AND is_deleted = 0
          AND (
                (@user_id IS NOT NULL AND user_id = @user_id)
             OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
             OR (@anonymous_session_id IS NOT NULL AND anonymous_session_id = @anonymous_session_id)
          );
        RETURN;
    END

    -- ------------------------------------------------------------------------
    -- 3. ADD MESSAGE
    -- ------------------------------------------------------------------------
    IF (@flag = 'add_message')
    BEGIN
        -- Verify ownership first
        IF NOT EXISTS (
            SELECT 1 FROM tbl_ai_conversation 
            WHERE conversation_id = @conversation_id 
              AND is_deleted = 0
              AND (
                    (@user_id IS NOT NULL AND user_id = @user_id)
                 OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
                 OR (@anonymous_session_id IS NOT NULL AND anonymous_session_id = @anonymous_session_id)
              )
        )
        BEGIN
            SELECT 0 AS success, 'Access Denied: Conversation not owned by caller' AS message;
            RETURN;
        END

        -- Compute next sequence number for this conversation
        DECLARE @next_seq INT;
        SELECT @next_seq = ISNULL(MAX(sequence_number), 0) + 1 
        FROM tbl_ai_conversation_message 
        WHERE conversation_id = @conversation_id;

        INSERT INTO tbl_ai_conversation_message
        (
            conversation_id,
            client_message_id,
            role,
            message_text,
            message_type,
            structured_context_json,
            sequence_number,
            is_summary,
            created_date
        )
        VALUES
        (
            @conversation_id,
            @client_message_id,
            @role,
            @message_text,
            ISNULL(@message_type, 'text'),
            @structured_context_json,
            @next_seq,
            0,
            GETDATE()
        );

        -- Update last activity and title if it's the first user message and title is default
        UPDATE tbl_ai_conversation
        SET last_activity_date = GETDATE(),
            title = CASE 
                        WHEN (title = 'New Conversation' OR title IS NULL) AND @role = 'user' 
                        THEN SUBSTRING(@message_text, 1, 60) 
                        ELSE title 
                    END
        WHERE conversation_id = @conversation_id;

        SELECT 1 AS success, SCOPE_IDENTITY() AS message_id, @next_seq AS sequence_number;
        RETURN;
    END

    -- ------------------------------------------------------------------------
    -- 4. GET RECENT MESSAGES
    -- ------------------------------------------------------------------------
    IF (@flag = 'get_recent_messages')
    BEGIN
        IF @max_messages <= 0 OR @max_messages > 30
            SET @max_messages = 10;

        -- Verify ownership first
        IF NOT EXISTS (
            SELECT 1 FROM tbl_ai_conversation 
            WHERE conversation_id = @conversation_id 
              AND is_deleted = 0
              AND (
                    (@user_id IS NOT NULL AND user_id = @user_id)
                 OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
                 OR (@anonymous_session_id IS NOT NULL AND anonymous_session_id = @anonymous_session_id)
              )
        )
        BEGIN
            -- Return empty result set if unauthorized
            SELECT TOP 0 1 AS message_id;
            RETURN;
        END

        -- Retrieve latest N messages and order chronologically
        ;WITH RecentMsgs AS (
            SELECT TOP (@max_messages)
                m.message_id,
                m.conversation_id,
                m.role,
                m.message_text,
                m.message_type,
                m.structured_context_json,
                m.sequence_number,
                m.created_date
            FROM tbl_ai_conversation_message m WITH (NOLOCK)
            WHERE m.conversation_id = @conversation_id
            ORDER BY m.sequence_number DESC
        )
        SELECT * FROM RecentMsgs
        ORDER BY sequence_number ASC;

        RETURN;
    END

    -- ------------------------------------------------------------------------
    -- 5. UPDATE SUMMARY
    -- ------------------------------------------------------------------------
    IF (@flag = 'update_summary')
    BEGIN
        UPDATE tbl_ai_conversation
        SET summary = @summary,
            updated_date = GETDATE()
        WHERE conversation_id = @conversation_id
          AND is_deleted = 0
          AND (
                (@user_id IS NOT NULL AND user_id = @user_id)
             OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
             OR (@anonymous_session_id IS NOT NULL AND anonymous_session_id = @anonymous_session_id)
          );

        SELECT @@ROWCOUNT AS updated_rows;
        RETURN;
    END

    -- ------------------------------------------------------------------------
    -- 6. RESET / CLEAR CONVERSATION
    -- ------------------------------------------------------------------------
    IF (@flag = 'reset_conversation')
    BEGIN
        UPDATE tbl_ai_conversation
        SET status = 'Closed',
            is_active = 0,
            updated_date = GETDATE()
        WHERE conversation_id = @conversation_id
          AND (
                (@user_id IS NOT NULL AND user_id = @user_id)
             OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
             OR (@anonymous_session_id IS NOT NULL AND anonymous_session_id = @anonymous_session_id)
          );

        SELECT @@ROWCOUNT AS reset_rows;
        RETURN;
    END

    -- ------------------------------------------------------------------------
    -- 7. GET / SET USER PREFERENCE (Level 3)
    -- ------------------------------------------------------------------------
    IF (@flag = 'get_preferences')
    BEGIN
        SELECT preference_id, category, preference_key, preference_value, source
        FROM tbl_ai_user_preference WITH (NOLOCK)
        WHERE is_active = 1
          AND (
                (@user_id IS NOT NULL AND user_id = @user_id)
             OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
          );
        RETURN;
    END

    IF (@flag = 'set_preference')
    BEGIN
        IF @pref_category IS NOT NULL AND @pref_key IS NOT NULL AND @pref_value IS NOT NULL
        BEGIN
            -- Update existing or insert new
            IF EXISTS (
                SELECT 1 FROM tbl_ai_user_preference 
                WHERE category = @pref_category 
                  AND preference_key = @pref_key
                  AND (
                        (@user_id IS NOT NULL AND user_id = @user_id)
                     OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
                  )
            )
            BEGIN
                UPDATE tbl_ai_user_preference
                SET preference_value = @pref_value,
                    updated_date = GETDATE()
                WHERE category = @pref_category 
                  AND preference_key = @pref_key
                  AND (
                        (@user_id IS NOT NULL AND user_id = @user_id)
                     OR (@user_id IS NULL AND anonymous_session_id = @anonymous_session_id)
                  );
            END
            ELSE
            BEGIN
                INSERT INTO tbl_ai_user_preference
                (user_id, anonymous_session_id, category, preference_key, preference_value, source)
                VALUES
                (@user_id, @anonymous_session_id, @pref_category, @pref_key, @pref_value, 'USER_EXPLICIT');
            END

            SELECT 1 AS success;
        END
        RETURN;
    END
END
GO
