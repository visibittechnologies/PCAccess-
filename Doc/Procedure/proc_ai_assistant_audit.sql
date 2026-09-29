-- ==============================================================================
-- Stored Procedure: proc_ai_assistant_audit
-- Description: Handles secure insertion and aggregation of AI audit logs and metrics.
-- ==============================================================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_ai_assistant_audit]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[proc_ai_assistant_audit]
GO

CREATE PROCEDURE [dbo].[proc_ai_assistant_audit]
    @Action VARCHAR(50),
    @CorrelationId VARCHAR(64) = NULL,
    @ConversationId VARCHAR(64) = NULL,
    @MessageId VARCHAR(64) = NULL,
    @UserId BIGINT = NULL,
    @AnonymousSessionId VARCHAR(100) = NULL,
    @UserIp VARCHAR(50) = NULL,
    @EventCategory VARCHAR(50) = NULL,
    @PromptSanitized NVARCHAR(1000) = NULL,
    @ToolsUsed VARCHAR(500) = NULL,
    @ExecutionDurationMs INT = NULL,
    @HttpStatus INT = NULL,
    @ErrorCode VARCHAR(50) = NULL,
    @ErrorMessage NVARCHAR(500) = NULL,
    @AiModel VARCHAR(50) = NULL,
    @EstimatedTokens INT = NULL,
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    -- ==========================================================================
    -- ACTION: INSERT_LOG
    -- ==========================================================================
    IF (@Action = 'INSERT_LOG')
    BEGIN
        INSERT INTO [dbo].[tbl_ai_audit_log]
        (
            [CorrelationId],
            [ConversationId],
            [MessageId],
            [UserId],
            [AnonymousSessionId],
            [UserIp],
            [EventCategory],
            [PromptSanitized],
            [ToolsUsed],
            [ExecutionDurationMs],
            [HttpStatus],
            [ErrorCode],
            [ErrorMessage],
            [AiModel],
            [EstimatedTokens],
            [CreatedDate]
        )
        VALUES
        (
            ISNULL(@CorrelationId, 'corr_' + CONVERT(VARCHAR(36), NEWID())),
            ISNULL(@ConversationId, 'conv_system'),
            @MessageId,
            @UserId,
            @AnonymousSessionId,
            @UserIp,
            ISNULL(@EventCategory, 'INFO'),
            @PromptSanitized,
            @ToolsUsed,
            @ExecutionDurationMs,
            @HttpStatus,
            @ErrorCode,
            @ErrorMessage,
            @AiModel,
            @EstimatedTokens,
            GETDATE()
        );

        SELECT SCOPE_IDENTITY() AS InsertedAuditId;
        RETURN;
    END

    -- ==========================================================================
    -- ACTION: GET_METRICS (Past 24 hours)
    -- ==========================================================================
    IF (@Action = 'GET_METRICS')
    BEGIN
        DECLARE @Since DATETIME = DATEADD(HOUR, -24, GETDATE());

        SELECT 
            COUNT(1) AS TotalRequests,
            SUM(CASE WHEN [EventCategory] = 'CHAT_REQUEST' AND [HttpStatus] = 200 THEN 1 ELSE 0 END) AS SuccessfulRequests,
            SUM(CASE WHEN [EventCategory] = 'RATE_LIMITED' OR [HttpStatus] = 429 THEN 1 ELSE 0 END) AS RateLimitedRequests,
            SUM(CASE WHEN [EventCategory] = 'PROMPT_INJECTION_BLOCKED' THEN 1 ELSE 0 END) AS PromptInjectionBlocked,
            SUM(CASE WHEN [EventCategory] = 'QUOTA_EXHAUSTED' THEN 1 ELSE 0 END) AS QuotaExhausted,
            SUM(CASE WHEN [HttpStatus] >= 500 OR [EventCategory] = 'SYSTEM_ERROR' THEN 1 ELSE 0 END) AS ErrorCount,
            AVG(CAST([ExecutionDurationMs] AS BIGINT)) AS AvgDurationMs,
            MAX([ExecutionDurationMs]) AS MaxDurationMs
        FROM [dbo].[tbl_ai_audit_log] WITH (NOLOCK)
        WHERE [CreatedDate] >= @Since;

        RETURN;
    END

    -- ==========================================================================
    -- ACTION: PURGE_OLD_LOGS (Log retention cleanup)
    -- ==========================================================================
    IF (@Action = 'PURGE_OLD_LOGS')
    BEGIN
        DECLARE @Cutoff DATETIME = DATEADD(DAY, -ABS(@RetentionDays), GETDATE());

        DELETE FROM [dbo].[tbl_ai_audit_log]
        WHERE [CreatedDate] < @Cutoff;

        SELECT @@ROWCOUNT AS RowsDeleted;
        RETURN;
    END
END
GO
