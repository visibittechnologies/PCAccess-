using System;

namespace PCAccess.Models.AiAssistant
{
    /// <summary>
    /// Normalized application-level knowledge item.
    /// Exposes only safe, public website knowledge and strictly strips raw database metadata.
    /// </summary>
    public class KnowledgeItem
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public string SourceType { get; set; }
        public string Author { get; set; }
        public string PublishedDate { get; set; }
        public string CropName { get; set; }
        public string MandiName { get; set; }
        public string Price { get; set; }
        public string Location { get; set; }
    }
}
