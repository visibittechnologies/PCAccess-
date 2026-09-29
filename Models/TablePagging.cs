using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Web;

namespace PCAccess.Models
{
    public class Dashboard
    {
        public int PageSize { get; set; }
        public int PageNo { get; set; }
        public string sSortColName { get; set; } = string.Empty;
        public string sSortDir_0 { get; set; } = "asc";
    }

    public class SearchModel
    {        
        public string strValue1 { get; set; }
        public string strValue2 { get; set; }
        public string strValue3 { get; set; }
        public string strValue4 { get; set; }
        public string strValue5 { get; set; }
        public string strValue6 { get; set; }

        public int iValue1 { get; set; }
        public int iValue2 { get; set; }
        public int iValue3 { get; set; }
        public int iValue4 { get; set; }
        public int iValue5 { get; set; }
        public int iValue6 { get; set; }

        public long lValue1 { get; set; }
        public long lValue2 { get; set; }
        public long lValue3 { get; set; }
        public long lValue4 { get; set; }
        public long lValue5 { get; set; }
        public long lValue6 { get; set; }
    }
    public class JqDataTableModel : SearchModel
    {
        public string sEcho { get; set; }

        public string group_id { get; set; }

        public string sSearch { get; set; }
        public int iDisplayLength { get; set; }
        public int iDisplayStart { get; set; }

        public int? iColumns { get; set; }

        public int? iSortingCols { get; set; }
        public string sColumns { get; set; }

        public int iSortCol_0 { get; set; }
        public string sSortColName { get; set; }
        public string sSortDir_0 { get; set; }

        public string strOtherCriteria { get; set; }
        public string strValue1 { get; set; }
        public string strValue2 { get; set; }
        public string strValue3 { get; set; }
        public string strValue4 { get; set; }
        public string strValue5 { get; set; }
        public string strValue6 { get; set; }

        public int iValue1 { get; set; }
        public int iValue2 { get; set; }
        public int iValue3 { get; set; }
        public int iValue4 { get; set; }
        public int iValue5 { get; set; }
        public int iValue6 { get; set; }

        public string status { get; set; }

        public int roleId { get; set; }
       
        public int? log_type { get; set; }

        public DateTime? from_date { get; set; }

        public DateTime? to_date { get; set; }

        public string fromDate { get; set; }
        public string toDate { get; set; }

        public int categoryStatus { get; set; }
        public int BrandStatus { get; set; }

        
        public int categoryType { get; set; }
        public int propertyDataType { get; set; }
        public int propertyStatus { get; set; }

    }
}