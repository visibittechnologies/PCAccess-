using System;
using System.Collections.Generic;

namespace PCAccess.Models.AiAssistant.Tools
{
    /// <summary>
    /// Normalized tool execution request.
    /// Encapsulates incoming tool invocation from AI or internal orchestrator.
    /// </summary>
    public class AiToolRequest
    {
        public string ToolName { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public ToolPermissionLevel CallerPermission { get; set; }

        public AiToolRequest()
        {
            Arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            CallerPermission = ToolPermissionLevel.Public;
            RequestId = Guid.NewGuid().ToString("N");
        }

        public string GetStringArgument(string key, string defaultValue = "")
        {
            if (Arguments != null && Arguments.TryGetValue(key, out object val) && val != null)
            {
                return val.ToString().Trim();
            }
            return defaultValue;
        }

        public int GetIntArgument(string key, int defaultValue = 0)
        {
            if (Arguments != null && Arguments.TryGetValue(key, out object val) && val != null)
            {
                if (int.TryParse(val.ToString(), out int parsed))
                {
                    return parsed;
                }
            }
            return defaultValue;
        }

        public long GetLongArgument(string key, long defaultValue = 0)
        {
            if (Arguments != null && Arguments.TryGetValue(key, out object val) && val != null)
            {
                if (long.TryParse(val.ToString(), out long parsed))
                {
                    return parsed;
                }
            }
            return defaultValue;
        }
    }
}
