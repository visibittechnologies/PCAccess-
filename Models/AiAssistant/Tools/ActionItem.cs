namespace PCAccess.Models.AiAssistant.Tools
{
    /// <summary>
    /// Represents a structured interactive action returned by an AI tool for the client UI.
    /// Types are restricted to an explicit allowlist (e.g. "open_page", "open_blog", "dial_phone").
    /// </summary>
    public class ActionItem
    {
        public string Type { get; set; } // "open_page", "open_blog", "show_contact"
        public string Label { get; set; }
        public string Url { get; set; }
        public string Payload { get; set; }
    }
}
