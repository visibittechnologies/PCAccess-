using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PCAccess.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            int totalRows = 0;
            System.Data.DataTable dtReels = PCAccess.BAL.Company.GetPublicBlogList("Agri-Reels", null, null, 0, 10, out totalRows);
            ViewBag.TrendingReels = dtReels;

            System.Data.DataTable dtFeeds = PCAccess.BAL.Company.GetPublicBlogList("Social Feed", null, null, 0, 20, out totalRows);
            ViewBag.SocialFeeds = dtFeeds;

            return View();
        }
        public ActionResult Blog_details(long id = 0)
        {
            ViewBag.BlogId = id;
            return View();
        }
        public ActionResult Topics()
        {
            return View();
        }

        public ActionResult Cropguide()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetPublicBlogList(string blog_type, string category, string search, int pageNo = 1, int pageSize = 10)
        {
            try
            {
                int totalRows = 0;
                int offset = (pageNo - 1) * pageSize;
                System.Data.DataTable dt = PCAccess.BAL.Company.GetPublicBlogList(blog_type, category, search, offset, pageSize, out totalRows);
                return Json(new { 
                    status = "success", 
                    totalRecords = totalRows, 
                    data = Newtonsoft.Json.JsonConvert.SerializeObject(dt) 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Marketprice()
        {
            return View();
        }
        public ActionResult Farmingtechniques()
        {
            return View();
        }
        public ActionResult Sellproduct()
        {
            return View();
        }
        public ActionResult Governmentschemes()
        {
            return View();
        }
        public ActionResult Agritech()
        {
            return View();
        }
        public ActionResult NewsEvents()
        {
            return View();
        }
        public ActionResult About()
        {         
            return View();
        }

        public ActionResult Contact()
        {          
            return View();
        }

        public ActionResult SocialBadge()
        {
            int totalRows = 0;
            // Fetch Trending Reels (0-based offset)
            System.Data.DataTable dtReels = PCAccess.BAL.Company.GetPublicBlogList("Agri-Reels", null, null, 0, 10, out totalRows);
            ViewBag.TrendingReels = dtReels;

            // Fetch Social Feeds (0-based offset)
            System.Data.DataTable dtFeeds = PCAccess.BAL.Company.GetPublicBlogList("Social Feed", null, null, 0, 20, out totalRows);
            ViewBag.SocialFeeds = dtFeeds;

            return View();
        }
    }
}