using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using PCAccess.DAL;
using PCAccess.Models;

namespace PCAccess.BAL
{
    public class Account
    {

        public static DataTable VerifyUser(string username, string password, out string status, out string msg)
        {
            DataTable dt = new DataTable();
            try
            {
                SqlParameter outMsg = new SqlParameter("@msg", "")
                {
                    Direction = ParameterDirection.Output,
                    Size = 255
                };
                SqlParameter outStatus = new SqlParameter("@status", "")
                {
                    Direction = ParameterDirection.Output,
                    Size = 15
                };
                SqlParameter[] parameters =
                {
                        Parameters.GetStringParameter("username", username),
                        Parameters.GetStringParameter("password", password),
                        outMsg,
                        outStatus
                 };
                dt = MySqlHelper.ExecuteDataTable("proc_user_login", parameters);
                msg = outMsg.Value.ToString();
                status = outStatus.Value.ToString();
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Employee:EmployeeManager", username);
                msg = "Something went wrong";
                status = "failed";
            }
            return dt;
        }

        public static long GetUserIdByUsername(string username)
        {
            try
            {
                SqlParameter[] parameters =
                {
                 Parameters.GetStringParameter("username", username)
                };

                object result = MySqlHelper.ExecuteScalar(
                    @"SELECT user_id 
                     FROM tbl_user 
                     WHERE user_name = @username 
                     AND ISNULL(is_deleted,0)=0",
                    parameters
                );

                return result != null ? Convert.ToInt64(result) : 0;
            }
            catch
            {
                return 0;
            }
        }
        public static DataTable login_with_otp(LoginWithOtp objModel, string _ip, string _browser, out string status, out string msg)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter Paramsg = new SqlParameter("@msg", "");
                Paramsg.Direction = ParameterDirection.Output; Paramsg.Size = 255;

                SqlParameter Parastatus = new SqlParameter("@status", "");
                Parastatus.Direction = ParameterDirection.Output; Parastatus.Size = 200;
                SqlParameter[] parameters =
               {
                  Parameters.GetStringParameter("flag",objModel.flag),
                  Parameters.GetStringParameter("username",objModel.UserName!=null?objModel.UserName:""),
                  Parameters.GetStringParameter("otp",objModel.PassWord!=null?objModel.PassWord:""),
                  Parameters.GetStringParameter("token",objModel.token != null ? objModel.token : ""),
                  Parameters.GetIntParameter("user_role_id",objModel.user_role_id),
                  Parameters.GetStringParameter("app_type",objModel.app_type!=null?objModel.app_type:""),
                  Parameters.GetStringParameter("referral_id",objModel.referral_id!=null?objModel.referral_id:""),
                  Parastatus,
                  Paramsg,
                };

                dt = MySqlHelper.ExecuteDataTable("[dbo].[proc_login_with_otp]", parameters);
                msg = (string)Paramsg.Value;
                status = (string)Parastatus.Value;
            }
            catch (SqlException ex)
            { throw ex; }
            return dt;
        }
        public static DataTable login_with_otp_vendor(LoginWithOtp objModel, string _ip, string _browser, out string status, out string msg)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter Paramsg = new SqlParameter("@msg", "");
                Paramsg.Direction = ParameterDirection.Output; Paramsg.Size = 255;

                SqlParameter Parastatus = new SqlParameter("@status", "");
                Parastatus.Direction = ParameterDirection.Output; Parastatus.Size = 200;
                SqlParameter[] parameters =
               {
                  Parameters.GetStringParameter("flag",objModel.flag),
                  Parameters.GetStringParameter("username",objModel.UserName!=null?objModel.UserName:""),
                  Parameters.GetStringParameter("otp",objModel.PassWord!=null?objModel.PassWord:""),
                  Parameters.GetStringParameter("token",objModel.token != null ? objModel.token : ""),
                  Parameters.GetIntParameter("user_role_id",objModel.user_role_id),
                  Parameters.GetStringParameter("app_type",objModel.app_type!=null?objModel.app_type:""),
                  Parastatus,
                  Paramsg,
                };

                dt = MySqlHelper.ExecuteDataTable("[dbo].[proc_login_with_otp_vendor]", parameters);
                msg = (string)Paramsg.Value;
                status = (string)Parastatus.Value;
            }
            catch (SqlException ex)
            { throw ex; }
            return dt;
        }
        public static DataTable send_otp(ApiParametersModel model, out string status)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter Parastatus = new SqlParameter("@status", "");
                Parastatus.Direction = ParameterDirection.Output; Parastatus.Size = 200;

                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag",model.flag),
                    Parameters.GetStringParameter("auth_key",model.token!=null?model.token.ToString():""),
                    Parameters.GetStringParameter("otp",model.otp!=null? model.otp.ToString():""),
                    Parameters.GetStringParameter("app_type",model.app_type!=null? model.app_type.ToString():""),
                    Parastatus
                };
                dt = MySqlHelper.ExecuteDataTable("[dbo].[proc_otp_manager]", parameters);
                status = (string)Parastatus.Value;
                if (model.flag == "create_otp" && dt.Rows.Count > 0 && status == "success")
                {
                    string to_mobile_number = dt.Rows[0]["mobile_number"].ToString();
                    //string to_email_address = dt.Rows[0]["email"].ToString();
                    //string first_name = dt.Rows[0]["first_name"].ToString();
                    string send_otp = dt.Rows[0]["otp"].ToString();
                    string sms_for_otp = string.Format(myFunc.sms_for_otp, send_otp);
                    myFunc.sentsms(Convert.ToInt64(to_mobile_number), sms_for_otp);
                    ////-----Send On Email
                    //if (to_email_address != "")
                    //{
                    //    string emailTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "email_templates/verification/verification.html");
                    //    StreamReader str = new StreamReader(emailTemplatePath);
                    //    string mailText = str.ReadToEnd();
                    //    str.Close();
                    //    mailText = mailText.Replace("[USER_FULL_NAME]", first_name);
                    //    mailText = mailText.Replace("[OTP]", send_otp);
                    //    myFunc.SEND_HTML_MAIL_GMAIL(to_email_address, first_name, "Your One-Time Password (OTP)", mailText);
                    //}
                }
            }
            catch (SqlException ex)
            { throw ex; }
            return dt;
        }
    }
}