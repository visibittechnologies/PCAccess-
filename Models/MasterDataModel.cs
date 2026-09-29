using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCAccess.Models
{
    public class MasterDataModel
    {
        public string flag { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public string Search { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public string FilterJson { get; set; }
        public long UserId { get; set; }
        public string ProcedureName { get; set; }
    }
}