using System;
using System.Text.RegularExpressions;

namespace PCAccess.Models
{
    public static class Helpers
    {
        public static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            string slug = title.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^\w\s-]", "");
            slug = Regex.Replace(slug, @"[\s-]+", " ").Trim();
            slug = slug.Replace(" ", "-");

            return slug;
        }
  
    }
}
