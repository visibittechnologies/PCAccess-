using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PCAccess.DAL;
using PCAccess.Models;

namespace PCAccess.BAL
{
    /// <summary>
    /// Business Access Layer manager for connected PC devices and pairing.
    /// WHAT: Encapsulates all device registration, status transitions, pairing verification, and querying.
    /// REASON: Follows the project's strict architecture where controllers delegate all business logic to BAL,
    /// and all database operations strictly use PCAccess.DAL.MySqlHelper with Stored Procedures and Parameters helper.
    /// </summary>
    public static class DeviceManager
    {
        /// <summary>
        /// Generates a random 6-digit pairing code (e.g. 849-210) with 10-minute expiry.
        /// REASON: Allows simple, secure pairing between desktop Agent and web user account.
        /// </summary>
        public static string GeneratePairingCode(long userId, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                Random rnd = new Random();
                int part1 = rnd.Next(100, 999);
                int part2 = rnd.Next(100, 999);
                string code = part1.ToString() + "-" + part2.ToString();

                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 1),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetStringParameter("pairing_code", code),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_device_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (status == "success")
                {
                    return code;
                }
                return null;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Validates pairing code and registers a new device, issuing a persistent token.
        /// REASON: Action 2 in proc_device_manager; links device to user without passing user passwords.
        /// </summary>
        public static DeviceModel RegisterDevice(string pairingCode, Guid deviceGuid, string deviceName, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                string secretToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 2),
                    Parameters.GetStringParameter("pairing_code", pairingCode),
                    new SqlParameter("@device_guid", SqlDbType.UniqueIdentifier) { Value = deviceGuid },
                    Parameters.GetStringParameter("device_name", deviceName),
                    Parameters.GetStringParameter("device_token", secretToken),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_device_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (status == "success" && dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new DeviceModel
                    {
                        id = Convert.ToInt32(row["id"]),
                        device_guid = (Guid)row["device_guid"],
                        device_name = row["device_name"].ToString(),
                        user_id = Convert.ToInt64(row["user_id"]),
                        device_token = row["device_token"].ToString(),
                        status = row["status"].ToString(),
                        last_seen = Convert.ToDateTime(row["last_seen"])
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Authenticates an existing device using DeviceGuid and Token.
        /// REASON: Action 3 in proc_device_manager; fast credential validation for reconnecting agents.
        /// </summary>
        public static DeviceModel AuthenticateDevice(Guid deviceGuid, string deviceToken, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 3),
                    new SqlParameter("@device_guid", SqlDbType.UniqueIdentifier) { Value = deviceGuid },
                    Parameters.GetStringParameter("device_token", deviceToken),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_device_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (status == "success" && dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new DeviceModel
                    {
                        id = Convert.ToInt32(row["id"]),
                        device_guid = (Guid)row["device_guid"],
                        device_name = row["device_name"].ToString(),
                        user_id = Convert.ToInt64(row["user_id"]),
                        status = row["status"].ToString(),
                        last_seen = Convert.ToDateTime(row["last_seen"])
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Updates device online/offline status and SignalR connection ID.
        /// REASON: Action 4 in proc_device_manager; invoked on SignalR connect/disconnect events.
        /// </summary>
        public static DeviceModel UpdateStatus(Guid deviceGuid, string statusVal, string connectionId, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 4),
                    new SqlParameter("@device_guid", SqlDbType.UniqueIdentifier) { Value = deviceGuid },
                    Parameters.GetStringParameter("status_val", statusVal),
                    new SqlParameter("@connection_id", SqlDbType.NVarChar, 100) { Value = (object)connectionId ?? DBNull.Value },
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_device_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (status == "success" && dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new DeviceModel
                    {
                        id = Convert.ToInt32(row["id"]),
                        device_guid = (Guid)row["device_guid"],
                        device_name = row["device_name"].ToString(),
                        user_id = Convert.ToInt64(row["user_id"]),
                        status = row["status"].ToString(),
                        last_seen = Convert.ToDateTime(row["last_seen"])
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Records heartbeat from agent and updates last_seen timestamp.
        /// REASON: Action 5 in proc_device_manager; keeps device active and alive in DB.
        /// </summary>
        public static bool RecordHeartbeat(Guid deviceGuid)
        {
            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 5),
                    new SqlParameter("@device_guid", SqlDbType.UniqueIdentifier) { Value = deviceGuid },
                    paramStatus,
                    paramMsg
                };

                MySqlHelper.ExecuteNonQuery("proc_device_manager", parameters);
                string status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                return status == "success";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves all registered devices belonging to a user for dashboard rendering.
        /// REASON: Action 6 in proc_device_manager; replaces mock data with real database records.
        /// </summary>
        public static List<DeviceModel> GetUserDevices(long userId)
        {
            List<DeviceModel> list = new List<DeviceModel>();

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 6),
                    Parameters.GetLongParameter("user_id", userId),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_device_manager", parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new DeviceModel
                        {
                            id = Convert.ToInt32(row["id"]),
                            device_guid = (Guid)row["device_guid"],
                            device_name = row["device_name"].ToString(),
                            user_id = Convert.ToInt64(row["user_id"]),
                            status = row["status"].ToString(),
                            last_seen = Convert.ToDateTime(row["last_seen"]),
                            created_at = Convert.ToDateTime(row["created_at"]),
                            seconds_since_last_seen = Convert.ToInt32(row["seconds_since_last_seen"])
                        });
                    }
                }
            }
            catch
            {
                // Fallback returns empty list
            }

            return list;
        }
    }
}
