using System.Collections.Generic;

namespace PCAccess.Models.AiAssistant.Tools
{
    /// <summary>
    /// Normalized tool execution output.
    /// Categorizes results into FOUND, NOT_FOUND, VALIDATION_ERROR, UNAUTHORIZED, FORBIDDEN, or SYSTEM_ERROR.
    /// Never leaks internal exceptions or raw database schemas.
    /// </summary>
    public class AiToolResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } // "FOUND", "NOT_FOUND", "VALIDATION_ERROR", "UNAUTHORIZED", "FORBIDDEN", "TOOL_DISABLED", "SYSTEM_ERROR"
        public string ToolName { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public List<ActionItem> Actions { get; set; }
        public long ExecutionTimeMs { get; set; }
        public string ErrorCode { get; set; }

        public AiToolResult()
        {
            Actions = new List<ActionItem>();
            Success = false;
            Status = "NOT_FOUND";
        }

        public static AiToolResult Found(string toolName, string message, object data, List<ActionItem> actions = null, long elapsedMs = 0)
        {
            return new AiToolResult
            {
                Success = true,
                Status = "FOUND",
                ToolName = toolName,
                Message = message,
                Data = data,
                Actions = actions ?? new List<ActionItem>(),
                ExecutionTimeMs = elapsedMs
            };
        }

        public static AiToolResult NotFound(string toolName, string message = "No matching information found.", long elapsedMs = 0)
        {
            return new AiToolResult
            {
                Success = true,
                Status = "NOT_FOUND",
                ToolName = toolName,
                Message = message,
                ExecutionTimeMs = elapsedMs
            };
        }

        public static AiToolResult ValidationError(string toolName, string message, long elapsedMs = 0)
        {
            return new AiToolResult
            {
                Success = false,
                Status = "VALIDATION_ERROR",
                ToolName = toolName,
                Message = message,
                ErrorCode = "ERR_VALIDATION",
                ExecutionTimeMs = elapsedMs
            };
        }

        public static AiToolResult Unauthorized(string toolName, string message = "This action is not permitted.", long elapsedMs = 0)
        {
            return new AiToolResult
            {
                Success = false,
                Status = "UNAUTHORIZED",
                ToolName = toolName,
                Message = message,
                ErrorCode = "ERR_UNAUTHORIZED",
                ExecutionTimeMs = elapsedMs
            };
        }

        public static AiToolResult Forbidden(string toolName, string message = "Requested tool does not exist or access is forbidden.", long elapsedMs = 0)
        {
            return new AiToolResult
            {
                Success = false,
                Status = "FORBIDDEN",
                ToolName = toolName,
                Message = message,
                ErrorCode = "ERR_FORBIDDEN",
                ExecutionTimeMs = elapsedMs
            };
        }

        public static AiToolResult SystemError(string toolName, string safeMessage = "Sorry, I could not complete that request right now.", long elapsedMs = 0)
        {
            return new AiToolResult
            {
                Success = false,
                Status = "SYSTEM_ERROR",
                ToolName = toolName,
                Message = safeMessage,
                ErrorCode = "ERR_SYSTEM",
                ExecutionTimeMs = elapsedMs
            };
        }
    }
}
