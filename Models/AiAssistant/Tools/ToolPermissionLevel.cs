namespace PCAccess.Models.AiAssistant.Tools
{
    /// <summary>
    /// Defines permission levels for AI Tools.
    /// V1 Public Assistant is strictly restricted to 'Public' tools only.
    /// </summary>
    public enum ToolPermissionLevel
    {
        Public = 1,
        Authenticated = 2,
        Admin = 3,
        System = 4
    }
}
