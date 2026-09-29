using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PCAccess.Models.AiAssistant.Gemini;

namespace PCAccess.Models.AiAssistant.Conversation
{
    /// <summary>
    /// Encapsulates the caller's server-verified identity.
    /// Strictly derived on the server (from encrypted auth cookie or secure anonymous cookie).
    /// </summary>
    public class UserIdentityContext
    {
        public long? UserId { get; set; }
        public string AnonymousSessionId { get; set; }
        public bool IsAuthenticated => UserId.HasValue && UserId.Value > 0;
        public string DisplayName { get; set; }
        public string ClientIp { get; set; }

        public override string ToString()
        {
            return IsAuthenticated ? $"User:{UserId}" : $"Guest:{AnonymousSessionId}";
        }
    }

    /// <summary>
    /// Compact structured item reference from previous tool output (e.g., blog search result).
    /// Allows the AI to reliably resolve ordinal queries like 'second wala' or 'first one'.
    /// </summary>
    public class StructuredContextReference
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("type")]
        public string ReferenceType { get; set; } = "blog";

        [JsonProperty("id")]
        public long ReferenceId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Entity representing an individual message turn in a conversation.
    /// </summary>
    public class AiConversationMessage
    {
        public long MessageId { get; set; }
        public string ConversationId { get; set; }
        public string ClientMessageId { get; set; }
        public string Role { get; set; }
        public string MessageText { get; set; }
        public string MessageType { get; set; } = "text";
        public string StructuredContextJson { get; set; }
        public int SequenceNumber { get; set; }
        public bool IsSummary { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public List<StructuredContextReference> GetParsedReferences()
        {
            if (string.IsNullOrWhiteSpace(StructuredContextJson))
                return new List<StructuredContextReference>();

            try
            {
                return JsonConvert.DeserializeObject<List<StructuredContextReference>>(StructuredContextJson)
                       ?? new List<StructuredContextReference>();
            }
            catch
            {
                return new List<StructuredContextReference>();
            }
        }
    }

    /// <summary>
    /// Entity representing a conversation session in tbl_ai_conversation.
    /// </summary>
    public class AiConversation
    {
        public string ConversationId { get; set; }
        public long? UserId { get; set; }
        public string AnonymousSessionId { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Snapshot of the prepared conversational context sent to Gemini.
    /// </summary>
    public class ContextWindow
    {
        public string ConversationId { get; set; }
        public string Summary { get; set; }
        public List<GeminiContent> Contents { get; set; } = new List<GeminiContent>();
        public List<StructuredContextReference> ActiveReferences { get; set; } = new List<StructuredContextReference>();
        public int EstimatedTokens { get; set; }
        public bool IsSummaryActive => !string.IsNullOrWhiteSpace(Summary);
    }

    /// <summary>
    /// Entity for Level 3 controlled user preferences.
    /// </summary>
    public class UserPreferenceItem
    {
        public long PreferenceId { get; set; }
        public string Category { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Source { get; set; } = "USER_EXPLICIT";
    }
}
