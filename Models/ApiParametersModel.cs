using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace PCAccess.Models
{
    public class ApiParametersModel
    {
        [JsonProperty("flag", NullValueHandling = NullValueHandling.Ignore)]
        public string flag { get; set; }

        [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 user_id { get; set; }
        
        [JsonProperty("CustomerId", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 CustomerId { get; set; }

        [JsonProperty("SaleId", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 SaleId { get; set; }
        [JsonProperty("PageId", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 PageId { get; set; }

        [JsonProperty("entry_by", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 entry_by { get; set; }

        [JsonProperty("reference_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceID { get; set; }

        [JsonProperty("PageNo", NullValueHandling = NullValueHandling.Ignore)]
        public int PageNo { get; set; }

        [JsonProperty("PageSize", NullValueHandling = NullValueHandling.Ignore)]
        public int PageSize { get; set; }

        [JsonProperty("searchcriteria", NullValueHandling = NullValueHandling.Ignore)]
        public string searchcriteria { get; set; }

        [JsonProperty("filter_list", NullValueHandling = NullValueHandling.Ignore)]
        public string filter_list { get; set; }

        [AllowHtml]
        [JsonProperty("add_update_jsonData", NullValueHandling = NullValueHandling.Ignore)]
        public string add_update_jsonData { get; set; }

        [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
        public string token { get; set; }

        [JsonProperty("otp", NullValueHandling = NullValueHandling.Ignore)]
        public string otp { get; set; }

        [JsonProperty("app_type", NullValueHandling = NullValueHandling.Ignore)]
        public string app_type { get; set; }
    }
}