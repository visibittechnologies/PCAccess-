using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PCAccess.BAL;
using PCAccess.Models;
using PCAccess.MyFun;
namespace PCAccess.Controllers
{
    public class CommonController : Controller
    {
        [HttpPost]
        public JsonResult GetCommonList(ApiParametersModel myModel)
        {
            try
            {
                string status = "failed", Msg = "";
                DataTable dt = MasterDataTabel.add_get_common_data(myModel, out status, out Msg);
                return Json(new { status = status, totalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["overall_count"].ToString()) : 0, data = JsonConvert.SerializeObject(dt) }, JsonRequestBehavior.AllowGet);
            }
            catch { return null; }
        }

        [HttpPost]
        public JsonResult CommonDelete(DeleteRequestModel model)
        {
            try
            {
                DataTable dt = MasterDataTabel.CommonDelete(model.Flag, model.Id);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string response = dt.Rows[0]["response"].ToString();
                    if (response == "success" && model.Flag == "remove_profile_image")
                    {
                        var currentUser = CommanUtilities.Provider.GetCurrent();
                        currentUser.profile_photo = "";
                        CommanUtilities.Provider.AddCurrent(currentUser);
                    }
                    return Json(new { status = response, message = "Record deleted successfully", url = "" });
                }
                return Json(new { status = "error", message = "Delete failed" });
            }
            catch
            {
                return Json(new { status = "error", message = "Server error" });
            }
        }
        public JsonResult GetAllDropDownValues(string type, string flag, int KeyId)
        {
            try
            {
                var ds = MasterDataTabel.GetAllDropDownValues(type, flag, KeyId);
                if (ds != null && ds.Tables.Count > 0)
                {
                    var data = ds.Tables[0].AsEnumerable().Select(row => ds.Tables[0].Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col]?.ToString())).ToList();
                    return Json(data, JsonRequestBehavior.AllowGet);
                }
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult SaveFile(HttpPostedFileBase file, string folder)
        {
            try
            {
                if (file == null || file.ContentLength <= 0)
                    return Json(new { status = "error", message = "No file selected" });
                string folderPath = Server.MapPath("~/Uploads/" + folder + "/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                string extension = Path.GetExtension(file.FileName);
                string fileName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(folderPath, fileName);
                file.SaveAs(fullPath);
                return Json(new { status = "success", picpath = "/Uploads/" + folder + "/" + fileName });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
      
        #region ProfileDetails
        [HttpPost]
        public JsonResult GetUserDetailsForEdit(AddUserDetails model)
        {
            string JSONresult = string.Empty;
            try
            {
                if (model.user_id <= 0) throw new Exception("Invalid Data");
                DataTable dt = Profle.GetUserDetailsForEdit("get_user_detail", model.user_id);
                List<AddUserDetails> userList = new List<AddUserDetails>();
                foreach (DataRow row in dt.Rows)
                {
                    AddUserDetails usr = new AddUserDetails();
                    usr.user_id = Convert.ToInt64(row["user_id"]);
                    usr.name = row["name"].ToString();
                    usr.email = row["email"].ToString();
                    usr.username = row["user_name"].ToString();
                    usr.password = row["password"].ToString();
                    usr.profile_photo = row["profile_photo"].ToString();
                    userList.Add(usr);
                }
                JSONresult = JsonConvert.SerializeObject(userList);
                return Json(JSONresult, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return null;
            }
        }
        [HttpPost]
        public JsonResult ProfileImageUpdate(AddUserDetails model)
        {
            try
            {
                long created_by = CommanUtilities.Provider.GetCurrent().user_id;
                if (model.ProfilePhotoImage != null && model.ProfilePhotoImage.ContentLength > 0)
                {
                    string childFolder = model.ChildPath ?? "profile";
                    string folderPath = Server.MapPath("~/UploadDoc/" + childFolder + "/");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                    string fileName = "profile_" + Guid.NewGuid() + Path.GetExtension(model.ProfilePhotoImage.FileName);
                    string fullPath = Path.Combine(folderPath, fileName);
                    model.ProfilePhotoImage.SaveAs(fullPath);
                    model.profile_photo = "/UploadDoc/" + childFolder + "/" + fileName;
                }
                string flag = "update_profile_image";
                DataTable dt = Profle.UpdateProfile(flag, model, created_by);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string response = dt.Rows[0]["response"].ToString();
                    if (response == "success")
                    {
                        var currentUser = CommanUtilities.Provider.GetCurrent();
                        currentUser.profile_photo = model.profile_photo;
                        CommanUtilities.Provider.AddCurrent(currentUser);
                    }
                    return Json(new { status = response, message = dt.Rows[0]["message"].ToString(), image_url = model.profile_photo });
                }
                return Json(new { status = "error", message = "Server error" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateProfile(AddUserDetails model)
        {
            long created_by = CommanUtilities.Provider.GetCurrent().user_id;
            try
            {
                string flag = "profile_update";
                DataTable dt = Profle.UpdateProfile(flag, model, created_by);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string response = dt.Rows[0]["response"].ToString();
                    if (response == "success" && model.isAuthChanged)
                    {
                        CommanUtilities.Provider.RemoveCurrent();
                        return Json(new { status = "logout", message = "Security details updated. Please login again.", url = "/Account/Login" });
                    }
                    if (response == "success")
                    {
                        var currentUser = CommanUtilities.Provider.GetCurrent();
                        currentUser.name = model.name;
                        // currentUser.profile_photo = model.profile_photo; // If profile update also includes photo update in this flag
                        CommanUtilities.Provider.AddCurrent(currentUser);
                    }
                    return Json(new { status = dt.Rows[0]["response"].ToString(), message = dt.Rows[0]["message"].ToString() });
                }
                return Json(new { status = "error", message = "Server error" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
        #endregion
        [HttpPost]
        public JsonResult Subscribe(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Email is required." });
                }
                string ipAddress = Request.UserHostAddress;
                var dt = PCAccess.BAL.Company.AddSubscriber(email, ipAddress);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string status = dt.Rows[0]["status"].ToString();
                    if (status == "Success")
                    {
                        return Json(new { success = true, message = "Thank you for subscribing!" });
                    }
                    else if (status == "Already Subscribed")
                    {
                        return Json(new { success = false, message = "You are already subscribed." });
                    }
                }
                return Json(new { success = false, message = "Something went wrong. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}