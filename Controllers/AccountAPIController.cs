namespace PCAccess.Controllers
{
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Web.Helpers;
    using System.Xml.Linq;
    using PCAccess.BAL;
    using PCAccess.Models;
    using PCAccess.MyFun;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Data.SqlClient;
    using PCAccess.DAL;
    using System.IO;
    public class AccountAPIController : Controller
    {

        // GET: AccountAPI
        [HttpPost]
        public JsonResult Login(LoginModel model)
        {
            string status = "failed", Msg = "";
            if (ModelState.IsValid)
            {
                string _id = Net.Ip, _browser = Net.BrowserDetails;
                DataTable dt = ActivityLogHelper.VerifyUser(model.UserName, model.PassWord, _id, _browser, out status, out Msg);
                if (status == "failed")
                {
                    return Json(new { status = status, message = Msg, url = "" }, 0);
                }
                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        OperatorModel op = new OperatorModel();
                        op = dt.AsEnumerable().Select(m => new OperatorModel
                        {
                            user_id = long.Parse(m["user_id"].ToString()),
                            name = m["name"].ToString(),
                            user_name = m["user_name"].ToString(),
                            email = m["email"].ToString(),
                            phone_no = m["phone_no"].ToString(),
                            user_type_id = int.Parse(m["user_type_id"].ToString()),
                            status = m["status"].ToString(),
                            GetUrl = dt.Columns.Contains("module_url") ? m["module_url"].ToString() : (dt.Columns.Contains("url") ? m["url"].ToString() : ""),
                            profile_photo = dt.Columns.Contains("profile_photo") ? m["profile_photo"].ToString() : "",
                            RoleName = dt.Columns.Contains("role_name") ? m["role_name"].ToString() : (dt.Columns.Contains("user_type") ? m["user_type"].ToString() : "Admin")
                        }).FirstOrDefault();

                        // Refresh profile photo from DB since login SP might not return it
                        DataTable dtExtra = Profle.GetUserDetailsForEdit("get_user_detail", op.user_id);
                        if (dtExtra != null && dtExtra.Rows.Count > 0)
                        {
                            op.profile_photo = dtExtra.Rows[0]["profile_photo"].ToString();
                        }

                        //op.UserMenus = Profle.getUserMenus(op.user_id);
                        CommanUtilities.Provider.AddCurrent(op);

                        // REMEMBER ME COOKIE
                        HttpCookie authCookie = new HttpCookie("PCAccessAuth");
                        authCookie.Value = op.user_id.ToString();
                        authCookie.HttpOnly = true;
                        authCookie.Secure = true;

                        if (model.RememberMe)
                            authCookie.Expires = DateTime.Now.AddDays(7);
                        else
                            authCookie.Expires = DateTime.Now.AddHours(2);

                        Response.Cookies.Add(authCookie);

                        return Json(new { status = status, message = Msg, url = op.GetUrl }, 0);
                    }
                }
            }
            return Json(new { status = status, message = "Invalid User", url = "" }, 0);
        }
        [HttpPost]
        public JsonResult LoginOut(LoginModel model)
        {
            CommanUtilities.Provider.RemoveCurrent();
            // 🔹 COOKIE CLEAR
            if (Request.Cookies["PCAccessAuth"] != null)
            {
                HttpCookie cookie = new HttpCookie("PCAccessAuth");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            return Json(new { status = true, message = "", url = "/Account/Login" }, 0);
        }
      
    }
}