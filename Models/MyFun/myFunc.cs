using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using PCAccess.BAL;
using PCAccess.Models;

namespace PCAccess.Models
{
    public class myFunc
    {
        public static string AdminEmailAddress = "abhicare15@gmail.com";
        public static string SenderAppPassword = "gyunlzhgekijjtll";
        public static string getURLNAME = "PCAccess";
        public static string URL = System.Configuration.ConfigurationManager.AppSettings["WebsiteURL"] ?? (System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Request.Url.GetLeftPart(System.UriPartial.Authority) + System.Web.HttpContext.Current.Request.ApplicationPath.TrimEnd('/') + "/" : "");
        public static decimal PlateformFee = 0;
        public static decimal DeliveryCharge(decimal orderAmount)
        {
            return orderAmount > 500 ? 0 : 70;
        }
        public static decimal Online_payment_discount = 0; //5 In %
  
        public static string randomNum(int Lenth)
        {
            string requiredCode = "1234567890";
            char[] passCharArray = new char[Lenth];
            Random r = new Random();
            for (int i = 0; i < Lenth; i++)
            {
                passCharArray[i] = requiredCode[(int)(requiredCode.Length * r.NextDouble())];
            }
            return new string(passCharArray);
        }
        public static Int32 getCartCookeisID
        {
            get
            {
                if (HttpContext.Current.Request.Cookies["CartCookeis"] != null)
                {
                    return Convert.ToInt32(HttpContext.Current.Request.Cookies["CartCookeis"].Value);
                }
                else
                {
                    string CookieID = myFunc.randomNum(7);
                    HttpCookie VER_PINCODE = new HttpCookie("CartCookeis");
                    VER_PINCODE.Value = CookieID;
                    VER_PINCODE.Expires = DateTime.Now.AddDays(15);
                    HttpContext.Current.Response.Cookies.Add(VER_PINCODE);
                    return Convert.ToInt32(CookieID);
                }
            }
            set
            {
                HttpCookie VER_PINCODE = new HttpCookie("CartCookeis");
                VER_PINCODE.Value = value.ToString();
                VER_PINCODE.Expires = DateTime.Now.AddDays(15);
                HttpContext.Current.Response.Cookies.Add(VER_PINCODE);
            }
        }

        #region encriptionDecription
        public static string EncryptionKey = "NKOSH_2_0@#admin@1132";
        public static string Encrypt(string clearText)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public static string Decrypt(string cipherText)
        {

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        public static char EncryptionInShortKey = 'D';
        public static string EncryptInShort(string plainText)
        {
            var output = new StringBuilder(plainText.Length);
            foreach (var character in plainText)
            {
                output.Append((char)(character ^ EncryptionInShortKey));
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(output.ToString()));
        }
        public static string DecryptInShort(string cipherText)
        {
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
                var output = new StringBuilder(decoded.Length);
                foreach (var character in decoded)
                {
                    output.Append((char)(character ^ EncryptionInShortKey));
                }
                return output.ToString();
            }
            catch{ return ""; }
        }
        #endregion
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
     
        public static string SEND_HTML_MAIL1_office365(string RECIRVER_EMAIL, string RECIRVER_NAME, string SUBJECT, string BODY)
        {
            try
            {
                string fromAddress = System.Configuration.ConfigurationManager.AppSettings["email_sender"];
                string fromPassword = System.Configuration.ConfigurationManager.AppSettings["email_sender_password"];
                string Host = System.Configuration.ConfigurationManager.AppSettings["host"];
                int port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["port"]);
                // smtp settings
                var smtp = new System.Net.Mail.SmtpClient();
                {
                    smtp.Host = Host;
                    smtp.Port = port;
                    smtp.EnableSsl = true;
                    smtp.UseDefaultCredentials = false;
                    smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtp.Credentials = new NetworkCredential(fromAddress, fromPassword);
                    smtp.Timeout = 600000;
                }
                // Passing values to smtp object
                using (var message = new MailMessage(fromAddress, RECIRVER_EMAIL)
                {
                    Subject = SUBJECT,
                    Body = BODY,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                //smtp.Send(fromAddress, toAddress, SUBJECT, BODY);
                return "true";
            }
            catch (Exception ee) { return ee.Message; }
        }
        #region SendSMS
        public const string tracking_url = "https://nefol.in/ot?oid={0}";
        //public const string sms_registration_login = "Thank you for taking interest in NKOSH! Your login OTP is {0} which will be valid for 5 mins. - NKOSH";//
        public const string sms_for_otp = "Your OTP verification code is {0}. This code is valid for 10 minutes. Please do not share it. NUTRIKOSH INDIA";
        public const string sms_refund = "Hi Refund of Rs. {0} credited to your Wallet against cancellation of your order with Booking id: {1} Thanks Team NKOSH.";
        public const string sms_order_delivered = "Hi Your Order with order Id {0}, was delivered successfully. Please check NKOSH App Order History for more details. Thanks Team NKOSH.";
        public const string sms_order_cancelled = "Hi Your Order with order Id {0}, was cancelled successfully. Visit NKOSH App Order History for more details. Thanks Team NKOSH.";
        public const string sms_new_order = "Hi Thank you for placing an order of Rs. {0}/- with knosh. Your order ID is {1}. Thanks Team NKOSH.";
        public static string sentsms(Int64 Mobno, string Message)
        {
            if(Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["is_sms_enabled"])==false)
            {
                return "Disabled";
            }
            //T_MasterCommandsModel objModel = new T_MasterCommandsModel();
            //objModel.CommandID = 45;//SMS
            //T_MasterCommandsModel res = objTran.getMasterCommand(2, objModel).FirstOrDefault();
            //if (res.CommandStatus == false)
            //{
            //    return "";
            //}

            string ret = string.Empty;
            string mbn = Mobno.ToString();
            //foreach (string str in Mobno)
            //{
            //    if (mbn == string.Empty)
            //    {
            //        mbn = str;
            //    }
            //    else
            //    {
            //        mbn = mbn + "," + str;
            //    }
            //}

            string AUTH_KEY = System.Configuration.ConfigurationManager.AppSettings["AUTH_KEY"];
            string senderId = System.Configuration.ConfigurationManager.AppSettings["senderId"];
            string api = "http://msg.msgclub.net/rest/services/sendSMS/sendGroupSms?AUTH_KEY="+ AUTH_KEY + "&message=" + Message + "&senderId="+ senderId + "&routeId=8&mobileNos=" + mbn + "&smsContentType=english";
            HttpWebRequest httpreq = (HttpWebRequest)WebRequest.Create(api);
            try
            {
                HttpWebResponse httpres = (HttpWebResponse)httpreq.GetResponse();
                StreamReader sr = new StreamReader(httpres.GetResponseStream());
                string results = sr.ReadToEnd();
                sr.Close();
                return results;
            }
            catch (Exception ee)
            {
                return ee.Message;
            }
        }
        #endregion SendSMS

        #region ConvertDataTableToList
        public static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }
        public static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                PropertyInfo property = temp.GetProperty(column.ColumnName);
                if (property != null && dr[column.ColumnName] != DBNull.Value)
                {
                    property.SetValue(obj, dr[column.ColumnName]);
                }
            }
            return obj;
        }
        #endregion
        public static string GetFileExtensionFromBase64(string base64String)
        {
            if (base64String.StartsWith("data:"))
            {
                int mimeIndex = base64String.IndexOf(";");
                if (mimeIndex != -1)
                {
                    string mimeType = base64String.Substring(5, mimeIndex - 5);
                    switch (mimeType)
                    {
                        case "image/jpeg": return ".jpg";
                        case "image/jpg": return ".jpg";
                        case "image/png": return ".png";
                        case "image/gif": return ".gif";
                        case "image/webp": return ".webp";
                        case "application/pdf": return ".pdf";

                        case "application/msword": return ".doc";
                        case "application/vnd.openxmlformats-officedocument.wordprocessingml.document": return ".docx";
                        case "application/vnd.ms-excel": return ".xls";
                        case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": return ".xlsx";
                        case "application/vnd.ms-powerpoint": return ".ppt";
                        case "application/vnd.openxmlformats-officedocument.presentationml.presentation":
                            return ".pptx";
                        case "text/plain": return ".txt";

                        case "application/zip": return ".zip";
                        case "application/x-rar-compressed": return ".rar";
                        case "application/x-7z-compressed": return ".7z";
                        case "application/gzip": return ".gz";

                        default: return ".bin";
                    }
                }
            }
            return ".bin";
        }
        //public static void send_web_notification(Int64 from_user_id, Int64 to_user_id, string message, string title,
        //string url, string style)
        //{
        //    try
        //    {
        //        ApiParametersModel objpost = new ApiParametersModel();
        //        string status = "failed", Msg = "";
        //        Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
        //        keyValuePairs.Add("from_user_id", from_user_id.ToString());
        //        keyValuePairs.Add("to_user_id", to_user_id.ToString());
        //        keyValuePairs.Add("message", message);
        //        keyValuePairs.Add("title", title);
        //        keyValuePairs.Add("url", url);
        //        keyValuePairs.Add("style", style);
        //        keyValuePairs.Add("entry_by", from_user_id.ToString());
        //        keyValuePairs.Add("access_by", to_user_id.ToString());
        //        objpost.flag = "add_notification";
        //        objpost.add_update_jsonData = "[" + JsonConvert.SerializeObject(keyValuePairs) + "]";
        //        DataTable DT = Admin.add_get_web_notifications(objpost, out status, out Msg);
        //    }
        //    catch { }
        //}

      
    }
}
