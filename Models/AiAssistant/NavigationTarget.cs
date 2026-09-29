namespace PCAccess.Models.AiAssistant
{
    /// <summary>
    /// Represents an authorized internal navigation target on the NKOSH Blog website.
    /// </summary>
    public class NavigationTarget
    {
        public string Route { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string Category { get; set; }
    }
}
