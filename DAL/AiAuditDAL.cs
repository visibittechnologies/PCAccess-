using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PCAccess.BAL;

namespace PCAccess.DAL
{
    /// <summary>
    /// Data Access Layer for persisting and querying AI Assistant security audit logs and performance metrics.
    /// Uses parameterized stored procedures with non-blocking fail-safes.
    /// </summary>
    public class AiAuditDAL
    {
        private const string SpName = "proc_ai_assistant_audit";

        public virtual long InsertAuditLog(
            string correlationId,
            string conversationId,
            string messageId,
            long? userId,
            string anonymousSessionId,
            string userIp,
            string eventCategory,
            string promptSanitized,
            string toolsUsed,
            int? executionDurationMs,
            int? httpStatus,
            string errorCode,
            string errorMessage,
            string aiModel,
            int? estimatedTokens)
        {
            try
            {
                var parameters = new List<SqlParameter>
                {
                    Parameters.GetStringParameter("Action", "INSERT_LOG"),
                    Parameters.GetStringParameter("CorrelationId", correlationId),
                    Parameters.GetStringParameter("ConversationId", conversationId),
                    Parameters.GetStringParameter("MessageId", messageId),
                    CreateNullableLongParam("UserId", userId),
                    Parameters.GetStringParameter("AnonymousSessionId", anonymousSessionId),
                    Parameters.GetStringParameter("UserIp", userIp),
                    Parameters.GetStringParameter("EventCategory", eventCategory ?? "INFO"),
                    Parameters.GetStringParameter("PromptSanitized", promptSanitized),
                    Parameters.GetStringParameter("ToolsUsed", toolsUsed),
                    CreateNullableIntParam("ExecutionDurationMs", executionDurationMs),
                    CreateNullableIntParam("HttpStatus", httpStatus),
                    Parameters.GetStringParameter("ErrorCode", errorCode),
                    Parameters.GetStringParameter("ErrorMessage", errorMessage),
                    Parameters.GetStringParameter("AiModel", aiModel),
                    CreateNullableIntParam("EstimatedTokens", estimatedTokens)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters.ToArray());
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["InsertedAuditId"] != DBNull.Value)
                {
                    return Convert.ToInt64(dt.Rows[0]["InsertedAuditId"]);
                }
            }
            catch (Exception ex)
            {
                // Non-blocking: Audit logging failure must never terminate the customer chat experience
                ExceptioLoggerDB.SaveErrorException(ex, "AiAuditDAL:InsertAuditLog", correlationId);
            }

            return 0;
        }

        public virtual DataTable GetMetrics24Hours()
        {
            try
            {
                var parameters = new List<SqlParameter>
                {
                    Parameters.GetStringParameter("Action", "GET_METRICS")
                };

                return MySqlHelper.ExecuteDataTable(SpName, parameters.ToArray());
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "AiAuditDAL:GetMetrics24Hours", string.Empty);
                return new DataTable();
            }
        }

        public virtual int PurgeOldLogs(int retentionDays = 30)
        {
            try
            {
                var parameters = new List<SqlParameter>
                {
                    Parameters.GetStringParameter("Action", "PURGE_OLD_LOGS"),
                    Parameters.GetIntParameter("RetentionDays", retentionDays)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters.ToArray());
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["RowsDeleted"] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0]["RowsDeleted"]);
                }
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "AiAuditDAL:PurgeOldLogs", retentionDays.ToString());
            }

            return 0;
        }

        private static SqlParameter CreateNullableLongParam(string paramName, long? val)
        {
            return new SqlParameter("@" + paramName.TrimStart('@'), SqlDbType.BigInt)
            {
                Value = val.HasValue ? (object)val.Value : DBNull.Value
            };
        }

        private static SqlParameter CreateNullableIntParam(string paramName, int? val)
        {
            return new SqlParameter("@" + paramName.TrimStart('@'), SqlDbType.Int)
            {
                Value = val.HasValue ? (object)val.Value : DBNull.Value
            };
        }
    }
}
