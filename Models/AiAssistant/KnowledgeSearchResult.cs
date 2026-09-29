using System;
using System.Collections.Generic;

namespace PCAccess.Models.AiAssistant
{
    /// <summary>
    /// Encapsulates the output of any knowledge retrieval operation.
    /// Distinguishes clearly between 'Found', 'NotFound', and 'Error' states to prevent hallucination.
    /// </summary>
    public class KnowledgeSearchResult
    {
        public string Status { get; set; } // "Found", "NotFound", "Error"
        public bool IsSuccess { get; set; }
        public string Query { get; set; }
        public KnowledgeType Type { get; set; }
        public int TotalFound { get; set; }
        public long ExecutionTimeMs { get; set; }
        public string ErrorMessage { get; set; }
        public List<KnowledgeItem> Items { get; set; }

        public KnowledgeSearchResult()
        {
            Items = new List<KnowledgeItem>();
            Status = "NotFound";
            IsSuccess = true;
        }

        public static KnowledgeSearchResult Found(List<KnowledgeItem> items, string query, KnowledgeType type, long elapsedMs)
        {
            return new KnowledgeSearchResult
            {
                Status = items != null && items.Count > 0 ? "Found" : "NotFound",
                IsSuccess = true,
                Query = query,
                Type = type,
                TotalFound = items != null ? items.Count : 0,
                Items = items ?? new List<KnowledgeItem>(),
                ExecutionTimeMs = elapsedMs
            };
        }

        public static KnowledgeSearchResult NotFound(string query, KnowledgeType type, long elapsedMs)
        {
            return new KnowledgeSearchResult
            {
                Status = "NotFound",
                IsSuccess = true,
                Query = query,
                Type = type,
                TotalFound = 0,
                Items = new List<KnowledgeItem>(),
                ExecutionTimeMs = elapsedMs
            };
        }

        public static KnowledgeSearchResult Error(string safeErrorMessage, string query, KnowledgeType type, long elapsedMs)
        {
            return new KnowledgeSearchResult
            {
                Status = "Error",
                IsSuccess = false,
                Query = query,
                Type = type,
                TotalFound = 0,
                ErrorMessage = safeErrorMessage,
                Items = new List<KnowledgeItem>(),
                ExecutionTimeMs = elapsedMs
            };
        }
    }
}
