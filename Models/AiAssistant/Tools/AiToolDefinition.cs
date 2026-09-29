using System.Collections.Generic;

namespace PCAccess.Models.AiAssistant.Tools
{
    /// <summary>
    /// Metadata definition for a registered AI Tool.
    /// Provides description, parameter schemas, and permission rules.
    /// </summary>
    public class AiToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // "Blog", "Category", "Company", "FAQ", "Navigation"
        public ToolPermissionLevel RequiredPermission { get; set; }
        public bool IsEnabled { get; set; }
        public List<AiToolParameter> Parameters { get; set; }

        public AiToolDefinition()
        {
            Parameters = new List<AiToolParameter>();
            RequiredPermission = ToolPermissionLevel.Public;
            IsEnabled = true;
        }
    }
}
