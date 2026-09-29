using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PCAccess.DAL;
using PCAccess.Models;

namespace PCAccess.BAL
{
    /// <summary>
    /// Business Access Layer for UserLoginAgent desktop client interactions.
    /// WHAT: Orchestrates user/employee authentication and permission-aware folder discovery.
    /// REASON: Adheres to the established BAL pattern, executing Stored Procedures with
    /// standard OUTPUT parameters and structured error handling.
    /// </summary>
    public static class UserAgentManager
    {
        /// <summary>
        /// Authenticates a user or employee from the desktop client agent.
        /// WHAT: Calls proc_user_agent_auth to validate credentials and retrieve accessible shared folders.
        /// REASON: Enables seamless direct desktop access for admins and employees without web browser login.
        /// </summary>
        public static UserAgentLoginResponse Authenticate(string usernameOrEmail, string password, string clientMachine, out string status, out string msg)
        {
            status = "failed";
            msg = "An unexpected error occurred.";

            try
            {
                SqlParameter pUsername = new SqlParameter("@username_or_email", SqlDbType.VarChar, 350)
                {
                    Value = (object)usernameOrEmail ?? DBNull.Value
                };

                SqlParameter pPassword = new SqlParameter("@password", SqlDbType.NVarChar, -1)
                {
                    Value = (object)password ?? DBNull.Value
                };

                SqlParameter pMachine = new SqlParameter("@client_machine", SqlDbType.NVarChar, 150)
                {
                    Value = string.IsNullOrWhiteSpace(clientMachine) ? (object)DBNull.Value : clientMachine
                };

                SqlParameter pStatus = new SqlParameter("@status", SqlDbType.VarChar, 20)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter pMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                DataSet ds = MySqlHelper.ExecuteDataSet(
                    "proc_user_agent_auth",
                    pUsername,
                    pPassword,
                    pMachine,
                    pStatus,
                    pMsg
                );

                status = pStatus.Value != null ? pStatus.Value.ToString() : "failed";
                msg = pMsg.Value != null ? pMsg.Value.ToString() : "";

                if (status == "success" && ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow userRow = ds.Tables[0].Rows[0];
                    int userTypeId = userRow["user_type_id"] != DBNull.Value ? Convert.ToInt32(userRow["user_type_id"]) : 2;
                    string roleName = userTypeId == 1 ? "Admin" : "User";

                    var response = new UserAgentLoginResponse
                    {
                        user_id = userRow["user_id"] != DBNull.Value ? Convert.ToInt64(userRow["user_id"]) : 0,
                        user_name = userRow["user_name"] != DBNull.Value ? userRow["user_name"].ToString() : "",
                        name = userRow["name"] != DBNull.Value ? userRow["name"].ToString() : "",
                        email = userRow["email"] != DBNull.Value ? userRow["email"].ToString() : "",
                        user_type_id = userTypeId,
                        role_name = roleName,
                        session_token = userRow["session_token"] != DBNull.Value ? userRow["session_token"].ToString() : Guid.NewGuid().ToString("N")
                    };

                    // Map Result Set 2 (Permitted Folders)
                    if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[1].Rows)
                        {
                            response.folders.Add(new UserAgentFolderDto
                            {
                                folder_id = row["folder_id"] != DBNull.Value ? Convert.ToInt64(row["folder_id"]) : 0,
                                folder_guid = row["folder_guid"] != DBNull.Value && Guid.TryParse(row["folder_guid"].ToString(), out Guid fg) ? fg : Guid.Empty,
                                folder_name = row["folder_name"] != DBNull.Value ? row["folder_name"].ToString() : "",
                                local_path = row["local_path"] != DBNull.Value ? row["local_path"].ToString() : "",
                                description = row["description"] != DBNull.Value ? row["description"].ToString() : "",
                                device_id = row["device_id"] != DBNull.Value ? Convert.ToInt32(row["device_id"]) : 0,
                                device_guid = row["device_guid"] != DBNull.Value && Guid.TryParse(row["device_guid"].ToString(), out Guid dg) ? dg : Guid.Empty,
                                device_name = row["device_name"] != DBNull.Value ? row["device_name"].ToString() : "",
                                device_status = row["device_status"] != DBNull.Value ? row["device_status"].ToString() : "offline",
                                is_device_online = row["is_device_online"] != DBNull.Value && Convert.ToInt32(row["is_device_online"]) == 1,
                                is_owner = row["is_owner"] != DBNull.Value && Convert.ToInt32(row["is_owner"]) == 1,
                                can_view = row["can_view"] != DBNull.Value && Convert.ToInt32(row["can_view"]) == 1,
                                can_download = row["can_download"] != DBNull.Value && Convert.ToInt32(row["can_download"]) == 1,
                                can_upload = row["can_upload"] != DBNull.Value && Convert.ToInt32(row["can_upload"]) == 1,
                                can_delete = row["can_delete"] != DBNull.Value && Convert.ToInt32(row["can_delete"]) == 1
                            });
                        }
                    }

                    return response;
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
    }
}
