using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCAccess.BAL;
using PCAccess.DAL;
using PCAccess.Models;
using PCAccess.MyFun;
using static PCAccess.MyFun.OperatorModel;

namespace PCAccess.Controllers
{
    public class AdminAPIController : Controller
    {
        string JSONresult = string.Empty;
        DataTable dt = new DataTable();
        long user_id = 0;

        #region Company Profile
        [HttpPost]
        public JsonResult AddCompanyProfile(AddUserDetails model)
        {
            try
            {
                user_id    = CommanUtilities.Provider.GetCurrent().user_id;
                dt         = Company.addCompanyProfile(string.IsNullOrEmpty(model.strValue1) ? "add" : model.strValue1, model.lValue1, user_id, model.JsonDataXML);
                JSONresult = JsonConvert.SerializeObject(dt);
            }
            catch { return null; }
            return Json(JSONresult, 0);
        }
        [HttpPost]
        public JsonResult GetCompanyProfileDetails(AddUserDetails model)
        {
            try
            {
                user_id    = CommanUtilities.Provider.GetCurrent().user_id;
                string flag = (model == null || string.IsNullOrEmpty(model.strValue1)) ? "get" : model.strValue1;
                dt         = Company.GetCompanyData(flag, user_id);
                JSONresult = JsonConvert.SerializeObject(dt);
            }
            catch { return null; }
            return Json(JSONresult, 0);
        }
        #endregion
        #region Dashboard Overview
        [HttpGet]
        //public JsonResult GetDashboardOverview()
        //{
        //    try
        //    {
        //        long user_id = CommanUtilities.Provider.GetCurrent().user_id;
        //        int totalRows = 0;
        //        DataSet dsSummary = Company.GetDashboardCounts(user_id);
        //        DataTable dtCounts = dsSummary.Tables.Count > 0 ? dsSummary.Tables[0] : new DataTable();
        //        DataTable dtContact = Company.GetContactRequestList(user_id, "", "id", "desc", 0, 5, out totalRows, "", "", "", 0);
        //        DataTable dtAssessment = Company.GetAssessmentRequestList("", "id", "desc", 0, 5, out totalRows, "", "", "list");
        //        DataTable dtReferral = Company.GetReferralRequestList(user_id, "", "id", "desc", 0, 5, out totalRows, "", "");
        //        DataTable dtCareer = Company.GetCareerRequestList("", "id", "desc", 0, 5, out totalRows, "", "");
        //        var result = new
        //        {
        //            status = "success",
        //            Counts = dtCounts,
        //            RecentContact = dtContact,
        //            RecentAssessment = dtAssessment,
        //            RecentReferral = dtReferral,
        //            RecentCareer = dtCareer
        //        };
        //        return Json(JsonConvert.SerializeObject(result), JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        #endregion

        #region Category Management
        [HttpPost]
        public JsonResult AddTestimonial(ApiParametersModel model)
        {
            try
            {
                user_id = CommanUtilities.Provider.GetCurrent().user_id;
                string status = "failed";
                string message = "";
                string json_data = "";

                if (!string.IsNullOrEmpty(model.add_update_jsonData))
                {
                    var jsonObj = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(model.add_update_jsonData);
                    if (jsonObj != null && jsonObj.Count > 0)
                    {
                        jsonObj[0]["user_id"] = user_id;
                        json_data = JsonConvert.SerializeObject(jsonObj[0]);
                    }
                }

                string flag = string.IsNullOrEmpty(model.flag) ? "add_category" : model.flag;
                dt = Company.addCategory(flag, model.CustomerId, user_id, json_data);

                if (dt != null && dt.Rows.Count > 0)
                {
                    status = dt.Columns.Contains("response") ? dt.Rows[0]["response"].ToString() : "success";
                    message = dt.Columns.Contains("message") ? dt.Rows[0]["message"].ToString() : "Category saved successfully.";
                }

                return Json(new { status = status, message = message, data = model.CustomerId });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetSubscriberList(JqDataTableModel model)
        {
            try
            {
                var dt = Company.GetSubscribersList(model);
                var totalRecords = dt != null && dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["overall_count"]) : 0;
                var result = new
                {
                    sEcho = model.sEcho,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    aaData = JsonConvert.SerializeObject(dt)
                };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
    }
}