namespace PCAccess.Models.AiAssistant.Tools
{
    /// <summary>
    /// Metadata defining an individual tool parameter.
    /// Used by Tool Registry and later converted into Gemini/OpenAI tool declarations in Phase 5.
    /// </summary>
    public class AiToolParameter
    {
        public string Name { get; set; }
        public string Type { get; set; } // "string", "integer", "boolean", "number"
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
    }
}
