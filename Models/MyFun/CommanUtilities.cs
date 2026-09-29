namespace PCAccess.MyFun
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web;

    public class OperatorModel
    {
        public long user_id { get; set; }
        public string user_name { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public int user_type_id { get; set; }
        public string phone_no { get; set; }
        public bool IsActive { get; set; }
        public string GetUrl { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public Int64 SoldBy { get; set; }
        public string auth_key { get; set; }
        public string profile_photo { get; set; }
        public string status { get; set; }
        public bool IsProfileUpdated { get; set; }
        public bool is_mobile_verified { get; set; }

        public bool RememberMe { get; set; }
        public List<userMenu> UserMenus { get; set; }

        public class userMenu
        {
            [JsonProperty("id")]
            public long id { get; set; }
            [JsonProperty("text")]
            public string text { get; set; }
            [JsonProperty("url")]
            public string url { get; set; }
            [JsonProperty("level")]
            public int level { get; set; }
            [JsonProperty("icon")]
            public string icon { get; set; }
            [JsonProperty("child_count")]
            public int child_count { get; set; }
            [JsonProperty("_parentId")]
            public long _parentId { get; set; }
            [JsonProperty("checked")]
            public bool Checked { get; set; }
            [JsonProperty("state")]
            public string state { get; set; }
            [JsonProperty("children")]
            public List<userMenu> children { get; set; }
        }
    }

    [Serializable]
    public class CommanUtilities
    {
        public static CommanUtilities Provider
        {
            get { return new CommanUtilities(); }
        }
        private string LoginUserKey = "vsdigiCRM_loginkey_2023";
        public OperatorModel GetCurrent()
        {
            OperatorModel operatorModel = new OperatorModel();
            if (!string.IsNullOrEmpty(WebHelper.GetCookie(LoginUserKey).ToString()))
                operatorModel = DESEncrypt.Decrypt(WebHelper.GetCookie(LoginUserKey).ToString()).ToObject<OperatorModel>();
            else
            {
                HttpContext.Current.Response.Write("<script language='javascript'> {alert('Session expired please login again.');top.window.location.href = top.window.location.origin + '/Account/Login' }</script>");
            }
            return operatorModel;
        }
        public void AddCurrent(OperatorModel operatorModel)
        {
            WebHelper.WriteCookie(LoginUserKey, DESEncrypt.Encrypt(operatorModel.ToJson()), 480);
        }
        public void RemoveCurrent()
        {
            WebHelper.RemoveCookie(LoginUserKey.Trim());
        }
    }
    public class WebHelper
    {
        #region Session operation
        /// <summary>
        /// Write Session
        /// </summary>
        /// <typeparam name="T">Session key type</typeparam>
        /// <param name="key">Session key name</param>
        /// <param name="value">Session key</param>
        public static void WriteSession<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            HttpContext.Current.Session[key] = value;
        }

        /// <summary>
        /// Write Session
        /// </summary>
        /// <param name="key">Session key name</param>
        /// <param name="value">Session key</param>
        public static void WriteSession(string key, string value)
        {
            WriteSession<string>(key, value);
        }

        /// <summary>
        /// Read the value of Session
        /// </summary>
        /// <param name="key">Session key name</param>        
        public static string GetSession(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            return HttpContext.Current.Session[key] as string;
        }
        /// <summary>
        /// Delete the specified Session
        /// </summary>
        ///// <param name="key">Session key name</param>
        public static void RemoveSession(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            HttpContext.Current.Session.Contents.Remove(key);
        }

        #endregion


        #region Cookie operation
        /// <summary>
        /// Write cookie value
        /// </summary>
        /// <param name="strName">name</param>
        /// <param name="strValue">value</param>
        public static void WriteCookie(string strName, string strValue)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[strName];
            if (cookie == null)
            {
                cookie = new HttpCookie(strName);
            }
            cookie.Value = strValue;
            HttpContext.Current.Response.AppendCookie(cookie);
        }
        /// <summary>
        /// Write cookie value
        /// </summary>
        /// <param name="strName">name</param>
        /// <param name="strValue">value</param>
        /// <param name="strValue">Expiration time (minutes)</param>
        public static void WriteCookie(string strName, string strValue, int expires)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[strName];
            if (cookie == null)
            {
                cookie = new HttpCookie(strName);
            }
            else
            {
                HttpContext.Current.Response.Cookies.Remove(strName);
                cookie = new HttpCookie(strName);
            }
            //cookie.Domain = HttpContext.Current.Request.Url.Host;
            cookie.Value = strValue;
            cookie.Expires = DateTime.Now.AddMinutes(expires);
            cookie.HttpOnly = true;
            cookie.Path = "/";
            HttpContext.Current.Response.AppendCookie(cookie);
            HttpContext.Current.Response.SetCookie(cookie);
        }
        /// <summary>
        /// Read cookie value
        /// </summary>
        /// <param name="strName">Name</param>
        /// <returns>cookie value</returns>
        public static string GetCookie(string strName)
        {
            if (HttpContext.Current.Request.Cookies != null && HttpContext.Current.Request.Cookies[strName] != null)
            {
                return HttpContext.Current.Request.Cookies[strName].Value.ToString();
            }
            return "";
        }
        /// <summary>
        /// Delete the cookie object
        /// </summary>
        /// <param name="CookiesName">Cookie object name</param>
        public static void RemoveCookie(string CookiesName)
        {
            HttpCookie objCookie = new HttpCookie(CookiesName.Trim());
            objCookie.Expires = DateTime.Now.AddYears(-5);
            HttpContext.Current.Response.Cookies.Add(objCookie);
        }
        #endregion
    }
    public class DESEncrypt
    {
        private static string DESKey = "DPOSCHOOL_desencrypt_2023";

        #region ======== Encryption ========
        /// <summary>
        /// encryption
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static string Encrypt(string Text)
        {
            return Encrypt(Text, DESKey);
        }
        /// <summary> 
        ///Encrypt data
        /// </summary> 
        /// <param name="Text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        public static string Encrypt(string Text, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray;
            inputByteArray = Encoding.Default.GetBytes(Text);
            des.Key = ASCIIEncoding.ASCII.GetBytes(System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            des.IV = ASCIIEncoding.ASCII.GetBytes(System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            StringBuilder ret = new StringBuilder();
            foreach (byte b in ms.ToArray())
            {
                ret.AppendFormat("{0:X2}", b);
            }
            return ret.ToString();
        }

        #endregion

        #region ======== Decrypt ========
        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static string Decrypt(string Text)
        {
            if (!string.IsNullOrEmpty(Text))
            {
                return Decrypt(Text, DESKey);
            }
            else
            {
                return "";
            }
        }
        /// <summary> 
        /// Decrypt the data
        /// </summary> 
        /// <param name="Text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        public static string Decrypt(string Text, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            int len;
            len = Text.Length / 2;
            byte[] inputByteArray = new byte[len];
            int x, i;
            for (x = 0; x < len; x++)
            {
                i = Convert.ToInt32(Text.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte)i;
            }
            des.Key = ASCIIEncoding.ASCII.GetBytes(System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            des.IV = ASCIIEncoding.ASCII.GetBytes(System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.Default.GetString(ms.ToArray());
        }

        #endregion
    }

    public static class Json
    {
        public static object ToJson(this string Json)
        {
            return Json == null ? null : JsonConvert.DeserializeObject(Json);
        }
        public static string ToJson(this object obj)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
            return JsonConvert.SerializeObject(obj, timeConverter);
        }
        public static string ToJson(this object obj, string datetimeformats)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = datetimeformats };
            return JsonConvert.SerializeObject(obj, timeConverter);
        }
        public static T ToObject<T>(this string Json)
        {
            return Json == null ? default(T) : JsonConvert.DeserializeObject<T>(Json);
        }
        public static List<T> ToList<T>(this string Json)
        {
            return Json == null ? null : JsonConvert.DeserializeObject<List<T>>(Json);
        }
        public static DataTable ToTable(this string Json)
        {
            return Json == null ? null : JsonConvert.DeserializeObject<DataTable>(Json);
        }
        //public static JObject ToJObject(this string Json)
        //{
        //    return Json == null ? JObject.Parse("{}") : JObject.Parse(Json.Replace("&nbsp;", ""));
        //}
    }
}