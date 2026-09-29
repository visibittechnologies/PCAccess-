using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PCAccess.BAL;
using PCAccess.Models.AiAssistant.Conversation;

namespace PCAccess.DAL
{
    /// <summary>
    /// Data Access Layer for AI Conversation Sessions and Message History.
    /// Interacts strictly with [dbo].[proc_ai_assistant_conversation] with ownership enforcement.
    /// </summary>
    public class ConversationDAL
    {
        private const string SpName = "proc_ai_assistant_conversation";

        private static SqlParameter CreateLongParam(string paramName, long? value)
        {
            return new SqlParameter("@" + paramName, SqlDbType.BigInt)
            {
                Value = (object)value ?? DBNull.Value
            };
        }

        public bool CreateOrValidateConversation(string conversationId, UserIdentityContext identity, string title, out AiConversation conversation)
        {
            conversation = null;
            if (string.IsNullOrWhiteSpace(conversationId))
                return false;

            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "create_or_validate"),
                    Parameters.GetStringParameter("conversation_id", conversationId),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId),
                    Parameters.GetStringParameter_Null("title", title)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    int isAuth = Convert.ToInt32(row["is_authorized"]);
                    if (isAuth == 1)
                    {
                        conversation = new AiConversation
                        {
                            ConversationId = row.Table.Columns.Contains("conversation_id") ? row["conversation_id"].ToString() : conversationId,
                            Title = row.Table.Columns.Contains("title") ? row["title"]?.ToString() : title,
                            Summary = row.Table.Columns.Contains("summary") ? row["summary"]?.ToString() : null,
                            Status = row.Table.Columns.Contains("status") ? row["status"]?.ToString() : "Active"
                        };
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:CreateOrValidateConversation", conversationId);
                return false;
            }
        }

        public AiConversation GetConversation(string conversationId, UserIdentityContext identity)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return null;

            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "get_conversation"),
                    Parameters.GetStringParameter("conversation_id", conversationId),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new AiConversation
                    {
                        ConversationId = row["conversation_id"].ToString(),
                        UserId = row["user_id"] != DBNull.Value ? (long?)Convert.ToInt64(row["user_id"]) : null,
                        AnonymousSessionId = row["anonymous_session_id"]?.ToString(),
                        Title = row["title"]?.ToString(),
                        Summary = row["summary"]?.ToString(),
                        Status = row["status"]?.ToString(),
                        CreatedDate = Convert.ToDateTime(row["created_date"]),
                        LastActivityDate = Convert.ToDateTime(row["last_activity_date"])
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:GetConversation", conversationId);
                return null;
            }
        }

        public long AddMessage(string conversationId, UserIdentityContext identity, string role, string messageText, string messageType = "text", string clientMessageId = null, string structuredContextJson = null)
        {
            if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(messageText))
                return 0;

            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "add_message"),
                    Parameters.GetStringParameter("conversation_id", conversationId),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId),
                    Parameters.GetStringParameter("role", role),
                    Parameters.GetStringParameter("message_text", messageText),
                    Parameters.GetStringParameter_Null("message_type", messageType),
                    Parameters.GetStringParameter_Null("client_message_id", clientMessageId),
                    Parameters.GetStringParameter_Null("structured_context_json", structuredContextJson)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    if (row.Table.Columns.Contains("success") && Convert.ToInt32(row["success"]) == 1)
                    {
                        return Convert.ToInt64(row["message_id"]);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:AddMessage", conversationId);
                return 0;
            }
        }

        public List<AiConversationMessage> GetRecentMessages(string conversationId, UserIdentityContext identity, int maxMessages = 10)
        {
            var list = new List<AiConversationMessage>();
            if (string.IsNullOrWhiteSpace(conversationId))
                return list;

            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "get_recent_messages"),
                    Parameters.GetStringParameter("conversation_id", conversationId),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId),
                    Parameters.GetIntParameter("max_messages", maxMessages)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new AiConversationMessage
                        {
                            MessageId = Convert.ToInt64(row["message_id"]),
                            ConversationId = row["conversation_id"].ToString(),
                            Role = row["role"].ToString(),
                            MessageText = row["message_text"].ToString(),
                            MessageType = row["message_type"]?.ToString(),
                            StructuredContextJson = row["structured_context_json"]?.ToString(),
                            SequenceNumber = Convert.ToInt32(row["sequence_number"]),
                            CreatedDate = Convert.ToDateTime(row["created_date"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:GetRecentMessages", conversationId);
                return list;
            }
        }

        public bool UpdateSummary(string conversationId, UserIdentityContext identity, string summary)
        {
            if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(summary))
                return false;

            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "update_summary"),
                    Parameters.GetStringParameter("conversation_id", conversationId),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId),
                    Parameters.GetStringParameter("summary", summary)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:UpdateSummary", conversationId);
                return false;
            }
        }

        public bool ResetConversation(string conversationId, UserIdentityContext identity)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return false;

            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "reset_conversation"),
                    Parameters.GetStringParameter("conversation_id", conversationId),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:ResetConversation", conversationId);
                return false;
            }
        }

        public List<UserPreferenceItem> GetUserPreferences(UserIdentityContext identity)
        {
            var list = new List<UserPreferenceItem>();
            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "get_preferences"),
                    Parameters.GetStringParameter("conversation_id", "pref"),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new UserPreferenceItem
                        {
                            PreferenceId = Convert.ToInt64(row["preference_id"]),
                            Category = row["category"]?.ToString(),
                            Key = row["preference_key"]?.ToString(),
                            Value = row["preference_value"]?.ToString(),
                            Source = row["source"]?.ToString()
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:GetUserPreferences", identity?.ToString());
                return list;
            }
        }

        public bool SetUserPreference(UserIdentityContext identity, string category, string key, string value)
        {
            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", "set_preference"),
                    Parameters.GetStringParameter("conversation_id", "pref"),
                    CreateLongParam("user_id", identity?.UserId),
                    Parameters.GetStringParameter_Null("anonymous_session_id", identity?.AnonymousSessionId),
                    Parameters.GetStringParameter("pref_category", category),
                    Parameters.GetStringParameter("pref_key", key),
                    Parameters.GetStringParameter("pref_value", value)
                };

                DataTable dt = MySqlHelper.ExecuteDataTable(SpName, parameters);
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "ConversationDAL:SetUserPreference", identity?.ToString());
                return false;
            }
        }
    }
}
