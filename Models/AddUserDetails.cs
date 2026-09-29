using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PCAccess.Models
{
    public class AddUserDetails
    {
        public long user_id { get; set; }
  
        public long lValue1 { get; set; }
        public long lValue2 { get; set; }
        public long lValue3 { get; set; }
        public long lValue4 { get; set; }
        public long lValue5 { get; set; }

        public long lValue6 { get; set; }
        public long lValue7 { get; set; }

        public long lValue8 { get; set; }

        public long lValue9 { get; set; }

        public long lValue10 { get; set; }

        public long lValue11 { get; set; }

        public string strValue1 { get; set; }
        public string UserDataXML { get; set; }

        public string name { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string username { get; set; }
        public string profile_photo { get; set; }
        public string JsonDataXML { get; set; }

        public string CareerDataXML { get; set; }

        public string ChildPath { get; set; }
        public bool isAuthChanged { get; set; }
        public HttpPostedFileBase ProfilePhotoImage { get; set; }
    }
       
}