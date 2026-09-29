using System;
using System.Collections.Generic;
using System.Web.Mvc;
using PCAccess.BAL;
using PCAccess.Models;
using PCAccess.MyFun;

namespace PCAccess.Controllers
{
    /// <summary>
    /// API Controller for Device Registration, Pairing, and Verification.
    /// WHAT: Exposes endpoints for web pairing code generation and desktop agent authentication.
    /// REASON: Follows the established AccountAPIController pattern in PCAccess, returning standardized JSON:
    /// { status: "success"|"error", message = "...", data = ... }
    /// </summary>
    public class DeviceAPIController : Controller
    {
        /// <summary>
        /// Generates a new 6-digit pairing code for the authenticated web user.
        /// REASON: Called from Dashboard "Add Device" modal via AJAX.
        /// </summary>
        [HttpPost]
        public JsonResult GeneratePairingCode()
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized. Please login again." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                string pairingCode = DeviceManager.GeneratePairingCode(user.user_id, out status, out msg);

                if (status == "success" && !string.IsNullOrEmpty(pairingCode))
                {
                    return Json(new
                    {
                        status = "success",
                        message = "Pairing code generated successfully.",
                        data = new
                        {
                            pairing_code = pairingCode,
                            expires_in_seconds = 600 // 10 minutes
                        }
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Failed to generate pairing code." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Registers and pairs a new PC Agent using the 6-digit pairing code.
        /// REASON: First-time setup endpoint invoked by FileAccessAgent desktop app.
        /// </summary>
        [HttpPost]
        public JsonResult Register(DevicePairingRequestModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.pairing_code) || model.device_guid == Guid.Empty || string.IsNullOrWhiteSpace(model.device_name))
                {
                    return Json(new { status = "error", message = "Invalid registration parameters." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                DeviceModel device = DeviceManager.RegisterDevice(model.pairing_code.Trim(), model.device_guid, model.device_name.Trim(), out status, out msg);

                if (status == "success" && device != null)
                {
                    return Json(new
                    {
                        status = "success",
                        message = "Device paired successfully.",
                        data = new
                        {
                            device_guid = device.device_guid,
                            device_name = device.device_name,
                            device_token = device.device_token
                        }
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Pairing failed. Code may be invalid or expired." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Verifies credentials of an already registered PC Agent on subsequent launches.
        /// REASON: Fast validation for FileAccessAgent before opening SignalR connection.
        /// </summary>
        [HttpPost]
        public JsonResult Verify(DeviceVerifyRequestModel model)
        {
            try
            {
                if (model == null || model.device_guid == Guid.Empty || string.IsNullOrWhiteSpace(model.device_token))
                {
                    return Json(new { status = "error", message = "Invalid verification parameters." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                DeviceModel device = DeviceManager.AuthenticateDevice(model.device_guid, model.device_token, out status, out msg);

                if (status == "success" && device != null)
                {
                    return Json(new
                    {
                        status = "success",
                        message = "Device verified successfully.",
                        data = new
                        {
                            device_guid = device.device_guid,
                            device_name = device.device_name,
                            status = device.status
                        }
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Device verification failed. Invalid credentials." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Retrieves the real-time list of devices for the logged-in web user.
        /// REASON: Endpoint for dashboard dynamic refreshes via AJAX.
        /// </summary>
        [HttpGet]
        public JsonResult GetDevices()
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized." }, JsonRequestBehavior.AllowGet);
                }

                List<DeviceModel> list = DeviceManager.GetUserDevices(user.user_id);
                return Json(new { status = "success", data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
