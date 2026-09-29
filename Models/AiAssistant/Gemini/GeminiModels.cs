using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCAccess.Models.AiAssistant.Tools;

namespace PCAccess.Models.AiAssistant.Gemini
{
    #region Chat Endpoint DTOs
    /// <summary>
    /// Incoming chat request from frontend (e.g., chatbot.js).
    /// </summary>
    public class AiChatRequest
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        [JsonProperty("clientMessageId")]
        public string ClientMessageId { get; set; }

        public string GetEffectiveConversationId()
        {
            return !string.IsNullOrWhiteSpace(ConversationId)
                ? ConversationId
                : (!string.IsNullOrWhiteSpace(SessionId) ? SessionId : "default_session");
        }
    }

    /// <summary>
    /// Outgoing structured chat response for the frontend, supporting text, blog cards, navigation, and actions.
    /// </summary>
    public class AiChatResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        [JsonProperty("messageId")]
        public string MessageId { get; set; }

        [JsonProperty("responseType")]
        public string ResponseType { get; set; } = "text";

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        [JsonProperty("actions")]
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();

        [JsonProperty("sources")]
        public List<string> Sources { get; set; } = new List<string>();

        [JsonProperty("toolsUsed")]
        public List<string> ToolsUsed { get; set; } = new List<string>();

        [JsonProperty("executionDurationMs")]
        public long ExecutionDurationMs { get; set; }

        [JsonProperty("promptVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string PromptVersion { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// Normalized blog card for frontend rich rendering.
    /// </summary>
    public class BlogCardItem
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        public long BlogId
        {
            get { return Id; }
            set { Id = value; }
        }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }
    }
    #endregion

    #region Gemini API Request DTOs
    public class GeminiRequest
    {
        [JsonProperty("contents")]
        public List<GeminiContent> Contents { get; set; } = new List<GeminiContent>();

        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public List<GeminiTool> Tools { get; set; }

        [JsonProperty("system_instruction", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiContent SystemInstruction { get; set; }

        [JsonProperty("generationConfig", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiGenerationConfig GenerationConfig { get; set; }
    }

    public class GeminiGenerationConfig
    {
        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.4;

        [JsonProperty("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 800;

        [JsonProperty("topP")]
        public double TopP { get; set; } = 0.95;
    }

    public class GeminiContent
    {
        [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
        public string Role { get; set; }

        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();

        public static GeminiContent User(string text)
        {
            return new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart> { new GeminiPart { Text = text } }
            };
        }

        public static GeminiContent System(string text)
        {
            return new GeminiContent
            {
                Parts = new List<GeminiPart> { new GeminiPart { Text = text } }
            };
        }
    }

    public class GeminiPart
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("functionCall", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiFunctionCall FunctionCall { get; set; }

        [JsonProperty("functionResponse", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiFunctionResponse FunctionResponse { get; set; }
    }

    public class GeminiFunctionCall
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("args")]
        public JObject Args { get; set; }
    }

    public class GeminiFunctionResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("response")]
        public object Response { get; set; }
    }

    public class GeminiTool
    {
        [JsonProperty("function_declarations")]
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new List<GeminiFunctionDeclaration>();
    }

    public class GeminiFunctionDeclaration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiSchema Parameters { get; set; }
    }

    public class GeminiSchema
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "object";

        [JsonProperty("properties")]
        public Dictionary<string, GeminiSchemaProperty> Properties { get; set; } = new Dictionary<string, GeminiSchemaProperty>();

        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Required { get; set; } = new List<string>();
    }

    public class GeminiSchemaProperty
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
    #endregion

    #region Gemini API Response DTOs
    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public GeminiError Error { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }

        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }
    }

    public class GeminiError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
    #endregion
}
