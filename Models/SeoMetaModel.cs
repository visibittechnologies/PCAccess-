using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PCAccess.Models
{
    public class SeoMetaModel
    {
        public long user_id { get; set; }
        public int ID { get; set; }

        [Required(ErrorMessage = "Page name must be required.")]
        public string page_name { get; set; }

        [Required(ErrorMessage = "Page URL must be required.")]
        [RegularExpression(@"^(https?://.*|/.*|#.*)$", ErrorMessage = "Invalid URL format. Please enter a valid absolute or relative URL.")]
        public string page_url { get; set; }

        [Required(ErrorMessage = "Page title must be required.")]
        public string meta_title { get; set; }

        [Required(ErrorMessage = "Page keyword must be required.")]
        public string meta_keywords { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string meta_description { get; set; }

        public string google_script { get; set; }
    }
}