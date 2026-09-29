using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using PCAccess.DAL;

namespace PCAccess.BAL
{
    public static class ActivityLogHelper
    {
        public static DataTable VerifyUser(string username, string password, string _ip, string _browser, out string status, out string msg)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter Paramsg = new SqlParameter("@msg", "");
                Paramsg.Direction = ParameterDirection.Output; Paramsg.Size = 255;

                SqlParameter Parastatus = new SqlParameter("@status", "");
                Parastatus.Direction = ParameterDirection.Output; Parastatus.Size = 15;
                SqlParameter[] parameters =
               {
                  Parameters.GetStringParameter("username",username),
                  Parameters.GetStringParameter("password",password),
                  new SqlParameter("@id",_ip),
                  new SqlParameter("@browser",_browser),
                  Paramsg,
                  Parastatus
                };

                dt = MySqlHelper.ExecuteDataTable("proc_user_login", parameters);
                msg = (string)Paramsg.Value;
                status = (string)Parastatus.Value;
            }
            catch (SqlException ex)
            { throw ex; }
            return dt;
        }

        public static bool InsertActivityLog( long userId,int logType,string moduleUrl,string moduleName,string comment)
        {
            try
            {
                string ipAddress = GetClientIp();
                string browser = GetBrowserName();
                var location = GetLocation(ipAddress);

                SqlParameter[] parameters =
                {
                new SqlParameter("@user_id", userId),
                new SqlParameter("@log_type", logType),
                new SqlParameter("@module_url", moduleUrl),
                new SqlParameter("@module_name", moduleName),
                new SqlParameter("@description", comment),
                new SqlParameter("@ip_address", ipAddress),
                new SqlParameter("@browser", browser),
                new SqlParameter("@city", location.City),
                new SqlParameter("@country", location.Country),
                new SqlParameter("@login_date", DateTime.Now),
                new SqlParameter("@qflag", "I")
              };

                return MySqlHelper.ExecuteNonQuery(
                    "proc_ua_user_loginfo_iud",
                    parameters
                ) > 0;
            }
            catch
            {
                return false;
            }
        }

        // ================= IP =================
        private static string GetClientIp()
        {
            try
            {
                var request = HttpContext.Current?.Request;
                if (request == null) return "Unknown";

                string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrEmpty(ip))
                    return ip.Split(',')[0];

                return request.ServerVariables["REMOTE_ADDR"] ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        // ================= BROWSER =================
        private static string GetBrowserName()
        {
            try
            {
                var userAgent = HttpContext.Current?.Request.UserAgent;

                if (string.IsNullOrEmpty(userAgent))
                    return "Unknown";

                if (userAgent.Contains("Edg")) return "Edge";
                if (userAgent.Contains("Chrome")) return "Chrome";
                if (userAgent.Contains("Firefox")) return "Firefox";
                if (userAgent.Contains("Safari")) return "Safari";
                if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) return "Internet Explorer";

                return "Other";
            }
            catch
            {
                return "Unknown";
            }
        }

        // ================= LOCATION =================
        private static (string Country, string City) GetLocation(string ip)
        {
            try
            {
                if (string.IsNullOrEmpty(ip) || ip == "Unknown")
                    return ("Unknown", "Unknown");

                var request = WebRequest.Create($"http://ip-api.com/json/{ip}");
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = reader.ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    return (
                        data?.country ?? "Unknown",
                        data?.city ?? "Unknown"
                    );
                }
            }
            catch
            {
                return ("Unknown", "Unknown");
            }
        }
    }
}