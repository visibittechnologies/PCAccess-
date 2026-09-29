using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


namespace PCAccess.Controllers
{

    public class AccountController : Controller
    {

        public ActionResult Login()
        {
            return View();
        }
       
      
       
    }
}