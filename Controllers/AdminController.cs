using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PCAccess.BAL;
using PCAccess.Models;
using PCAccess.MyFun;

namespace PCAccess.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            return RedirectToAction("DashboardOverview");
        }

        public ActionResult DashboardOverview()
        {
            var model = BAL.DashboardManager.GetDashboardOverview();
            
            // REASON: Retrieve real devices and folders registered for the currently logged-in user
            PCAccess.MyFun.OperatorModel user = PCAccess.MyFun.CommanUtilities.Provider.GetCurrent();
            if (user != null && user.user_id > 0)
            {
                ViewBag.CurrentUserId = user.user_id;
                var devices = PCAccess.BAL.DeviceManager.GetUserDevices(user.user_id);
                var allFolders = PCAccess.BAL.FolderManager.GetAllUserSharedFolders(user.user_id);

                // Map real shared folder count to each device
                foreach (var dev in devices)
                {
                    dev.shared_folder_count = allFolders.Count(f => f.device_id == dev.id);
                }

                model.Devices = devices;
                model.SharedFolders = allFolders;
                model.TotalSharedFolders = allFolders.Count;
                model.OnlineDevicesCount = devices.Count(d => d.status == "online");
                ViewBag.DeviceList = devices;

                // Build real dynamic operational activities
                var activities = new List<DashboardActivityItemModel>();

                // 1. Online devices real-time status
                foreach (var dev in devices.Where(d => d.status == "online"))
                {
                    activities.Add(new DashboardActivityItemModel
                    {
                        Icon = "radio",
                        Title = $"{dev.device_name} is Online & Ready",
                        Description = $"SignalR active on {dev.device_name} (ID: {dev.device_guid.ToString().Substring(0, 8)}...)",
                        TimeAgo = dev.LastSeenFormatted,
                        BadgeColor = "emerald",
                        LinkUrl = $"/Admin/DeviceFolders?deviceGuid={dev.device_guid}"
                    });
                }

                // 2. Real configured shared folders
                foreach (var folder in allFolders.Take(3))
                {
                    activities.Add(new DashboardActivityItemModel
                    {
                        Icon = "folder-check",
                        Title = $"Shared folder: {folder.folder_name}",
                        Description = $"Host: {folder.device_name} &bull; Path: {folder.local_path}",
                        TimeAgo = folder.created_date.ToString("dd MMM yyyy"),
                        BadgeColor = "emerald",
                        LinkUrl = $"/Admin/FileBrowser?folderGuid={folder.folder_guid}"
                    });
                }

                // 3. Registered devices
                foreach (var dev in devices.OrderByDescending(d => d.created_at).Take(2))
                {
                    activities.Add(new DashboardActivityItemModel
                    {
                        Icon = "monitor",
                        Title = $"{dev.device_name} paired and authorized",
                        Description = $"Device ID: {dev.device_guid.ToString().Substring(0, 8)}... &bull; Status: {dev.status}",
                        TimeAgo = dev.created_at > DateTime.MinValue ? dev.created_at.ToString("dd MMM yyyy") : dev.LastSeenFormatted,
                        BadgeColor = dev.status == "online" ? "emerald" : "slate",
                        LinkUrl = $"/Admin/DeviceFolders?deviceGuid={dev.device_guid}"
                    });
                }

                if (activities.Count == 0)
                {
                    activities.Add(new DashboardActivityItemModel
                    {
                        Icon = "info",
                        Title = "No connected devices",
                        Description = "Click 'Connect PC' to pair your first computer.",
                        TimeAgo = "Just now",
                        BadgeColor = "slate"
                    });
                }

                model.RecentActivities = activities;
            }
            else
            {
                ViewBag.CurrentUserId = 0;
                ViewBag.DeviceList = new List<PCAccess.Models.DeviceModel>();
            }

            return View(model);
        }

        /// <summary>
        /// WHAT: Step 3 Shared Folder Management View for a specific device or default device.
        /// REASON: Allows device owner to view, add, edit, and configure permissions on local folders.
        /// </summary>
        public ActionResult DeviceFolders(string deviceGuid = "")
        {
            OperatorModel user = CommanUtilities.Provider.GetCurrent();
            if (user == null || user.user_id <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CurrentUserId = user.user_id;

            // Get user's connected devices
            var devices = DeviceManager.GetUserDevices(user.user_id);
            ViewBag.Devices = devices;

            DeviceModel selectedDevice = null;
            if (!string.IsNullOrWhiteSpace(deviceGuid))
            {
                selectedDevice = devices.FirstOrDefault(d => d.device_guid.ToString().Equals(deviceGuid, StringComparison.OrdinalIgnoreCase));
            }

            // Fallback to first available device if not specified or not found
            if (selectedDevice == null && devices.Count > 0)
            {
                selectedDevice = devices[0];
            }

            ViewBag.SelectedDevice = selectedDevice;

            // Load shared folders for the selected device
            List<SharedFolderModel> folders = new List<SharedFolderModel>();
            if (selectedDevice != null)
            {
                string status, msg;
                folders = FolderManager.GetDeviceFolders(user.user_id, selectedDevice.device_guid.ToString(), out status, out msg);
            }

            return View(folders);
        }

        /// <summary>
        /// WHAT: Dedicated Remote File & Folder Browser page.
        /// REASON: Step 4 - Displays interactive directory contents of a shared folder.
        /// URL: /Admin/FileBrowser?folderGuid={guid}&path={relativePath}
        /// </summary>
        public ActionResult FileBrowser(string folderGuid, string path = "")
        {
            OperatorModel user = CommanUtilities.Provider.GetCurrent();
            if (user == null || user.user_id <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(folderGuid) || !Guid.TryParse(folderGuid, out Guid guid))
            {
                return RedirectToAction("DashboardOverview");
            }

            string status, msg;
            FolderBrowseViewModel model = FolderManager.VerifyFolderViewPermission(user.user_id, guid, out status, out msg);

            if (model == null || status != "success")
            {
                TempData["ErrorMessage"] = msg ?? "You do not have permission to view this shared folder.";
                return RedirectToAction("DashboardOverview");
            }

            ViewBag.CurrentUserId = user.user_id;
            model.initial_path = path ?? "";
            return View(model);
        }

        public ActionResult CategoriesList()
        {
            return View();
        }
        public ActionResult AddCompany()
        {
            return View();
        }
         
        #region Google Script
        [HttpGet]
        public ActionResult GoogleScriptList(int id = 0)
        {
            SeoMetaModel model = new SeoMetaModel();
            if (id > 0)
            {
                model.ID = id;
                DataTable dt = Company.addGoogleScript("get", model);
                if (dt != null && dt.Rows.Count > 0)
                {
                    model.ID = Convert.ToInt32(dt.Rows[0]["ID"]);
                    model.google_script = dt.Rows[0]["ScriptContent"].ToString();
                }
            }
            ViewBag.ScriptList = Company.addGoogleScript("view", new SeoMetaModel());
            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult GoogleScript(SeoMetaModel OBJ)
        {
            try
            {
                OBJ.user_id = CommanUtilities.Provider.GetCurrent().user_id;
                string actionType = OBJ.ID > 0 ? "update" : "insert";
                DataTable dt = Company.addGoogleScript(actionType, OBJ);
                if (dt != null && dt.Rows.Count > 0)
                {
                    TempData["Message"] = OBJ.ID > 0 ? "Script updated successfully!" : "Script added successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;
            }
            return RedirectToAction("GoogleScriptList");
        }
        #endregion

        [HttpGet]
        public ActionResult PageMetaTagsList(int id = 0)
        {
            SeoMetaModel model = new SeoMetaModel();
            if (id > 0)
            {
                model.ID = id;
                DataTable dt = Company.addPageMetaTag("get", model);
                if (dt != null && dt.Rows.Count > 0)
                {
                    model.ID = Convert.ToInt32(dt.Rows[0]["ID"]);
                    model.page_name = dt.Rows[0]["page_name"].ToString();
                    model.page_url = dt.Rows[0]["page_url"].ToString();
                    model.meta_title = dt.Rows[0]["meta_title"].ToString();
                    model.meta_keywords = dt.Rows[0]["meta_keywords"].ToString();
                    model.meta_description = dt.Rows[0]["meta_description"].ToString();
                }
            }
            ViewBag.PageMetaTags = Company.addPageMetaTag("view", new SeoMetaModel());
            return View(model);
        }

        [HttpPost]
        public ActionResult PageMetaTags(SeoMetaModel OBJ)
        {
            try
            {
                OBJ.user_id = CommanUtilities.Provider.GetCurrent().user_id;
                string actionType = OBJ.ID > 0 ? "update" : "insert";
                DataTable dt = Company.addPageMetaTag(actionType, OBJ);
                if (dt != null && dt.Rows.Count > 0)
                {
                    TempData["Message"] = OBJ.ID > 0 ? "Page Meta Details updated successfully!" : "Page Meta Details added successfully!";
                }
                else
                {
                    TempData["Message"] = "No data returned from the database!";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;
            }
            return RedirectToAction("PageMetaTagsList");
        }

        public ActionResult Profile()
        {
            return View();
        }
  
        public ActionResult BlogList(long id = 0)
        {
            ViewBag.BlogId = id;
            ViewBag.KPI = Company.GetBlogKPI();
            return View();
        }

        public ActionResult SubscriberList()
        {
            return View();
        }

        #region Category API endpoints (Moved from AdminAPI to fix 404)
        [HttpPost]
        public JsonResult SaveCategory(ApiParametersModel model)
        {
            try
            {
                long user_id = CommanUtilities.Provider.GetCurrent().user_id;
                string status = "failed";
                string message = "";
                string json_data = "";

                if (!string.IsNullOrEmpty(model.add_update_jsonData))
                {
                    var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(model.add_update_jsonData);
                    if (jsonObj != null && jsonObj.Count > 0)
                    {
                        jsonObj[0]["user_id"] = user_id;
                        json_data = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj[0]);
                    }
                }

                string flag = string.IsNullOrEmpty(model.flag) ? "add_category" : model.flag;
                DataTable dt = Company.addCategory(flag, model.CustomerId, user_id, json_data);

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

        [HttpPost]
        public JsonResult GetCategoryForEdit(ApiParametersModel model)
        {
            try
            {
                long user_id = CommanUtilities.Provider.GetCurrent().user_id;
                DataTable dt = Company.GetCategoryDetails(model.CustomerId, user_id);
                string JSONresult = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                return Json(JSONresult, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
        #endregion
    }
}
