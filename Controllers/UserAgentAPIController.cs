using System;
using System.Web.Mvc;
using PCAccess.BAL;
using PCAccess.Models;

namespace PCAccess.Controllers
{
    /// <summary>
    /// API Controller for UserLoginAgent Desktop Client.
    /// WHAT: Exposes endpoints for user/employee authentication and folder synchronization.
    /// REASON: Follows the established project pattern (DeviceAPIController / AccountAPIController),
    /// returning standardized JSON: { status = "success"|"error", message = "...", data = ... }
    /// </summary>
    public class UserAgentAPIController : Controller
    {
        /// <summary>
        /// Direct authentication endpoint for UserLoginAgent.
        /// WHAT: Authenticates user by username/email and password, returning accessible folders.
        /// REASON: Allows users, employees, and admins to log in directly from the desktop agent
        /// without opening a web browser.
        /// </summary>
        [HttpPost]
        public JsonResult Login(UserAgentLoginRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.username_or_email) || string.IsNullOrWhiteSpace(req.password))
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Username/Email and Password are required."
                    }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                UserAgentLoginResponse response = UserAgentManager.Authenticate(
                    req.username_or_email.Trim(),
                    req.password,
                    req.client_machine,
                    out status,
                    out msg
                );

                if (status == "success" && response != null)
                {
                    return Json(new
                    {
                        status = "success",
                        message = "Login successful.",
                        data = response
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    status = "error",
                    message = !string.IsNullOrWhiteSpace(msg) ? msg : "Invalid credentials."
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = "error",
                    message = "Server error during authentication: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Lightweight connection test endpoint.
        /// WHAT: Verifies server connectivity from the UserLoginAgent before prompting for credentials.
        /// </summary>
        [HttpGet]
        public JsonResult Ping()
        {
            return Json(new
            {
                status = "success",
                message = "PCAccess Server is online and ready.",
                timestamp = DateTime.UtcNow
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
