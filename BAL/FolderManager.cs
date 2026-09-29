using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PCAccess.DAL;
using PCAccess.Models;

namespace PCAccess.BAL
{
    /// <summary>
    /// Business Access Layer manager for Shared Folders and Access Permissions.
    /// WHAT: Encapsulates folder registration, updates, soft removal, querying, and permission assignments.
    /// REASON: Follows project pattern: Controller -> BAL -> MySqlHelper -> Stored Procedure.
    /// Strictly verifies authenticated user ownership of parent device on every operation.
    /// </summary>
    public static class FolderManager
    {
        /// <summary>
        /// Registers a new shared folder for a connected device.
        /// REASON: ActionType 1 in proc_shared_folder_manager.
        /// </summary>
        public static SharedFolderModel AddSharedFolder(long userId, string deviceGuid, string folderName, string localPath, string description, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 1),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetStringParameter("device_guid", deviceGuid),
                    Parameters.GetStringParameter("folder_name", folderName),
                    Parameters.GetStringParameter("local_path", localPath),
                    Parameters.GetStringParameter("description", description ?? ""),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (status == "success" && dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    return new SharedFolderModel
                    {
                        folder_id = Convert.ToInt64(r["folder_id"]),
                        folder_guid = Guid.Parse(r["folder_guid"].ToString()),
                        device_id = Convert.ToInt32(r["device_id"]),
                        folder_name = r["folder_name"].ToString(),
                        local_path = r["local_path"].ToString(),
                        description = r["description"] != DBNull.Value ? r["description"].ToString() : "",
                        is_active = Convert.ToBoolean(r["is_active"])
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
        /// Updates shared folder metadata (Name, Description, is_active).
        /// REASON: ActionType 2 in proc_shared_folder_manager.
        /// </summary>
        public static bool UpdateSharedFolder(long userId, long folderId, string folderName, string description, bool isActive, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 2),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetLongParameter("folder_id", folderId),
                    Parameters.GetStringParameter("folder_name", folderName),
                    Parameters.GetStringParameter("description", description ?? ""),
                    Parameters.GetBooleanParameter("is_active", isActive),
                    paramStatus,
                    paramMsg
                };

                MySqlHelper.ExecuteNonQuery("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                return status == "success";
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Deactivates a shared folder record (Soft Delete).
        /// REASON: ActionType 3 in proc_shared_folder_manager. Physical folder on PC is never deleted.
        /// </summary>
        public static bool DeleteSharedFolder(long userId, long folderId, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 3),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetLongParameter("folder_id", folderId),
                    paramStatus,
                    paramMsg
                };

                MySqlHelper.ExecuteNonQuery("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                return status == "success";
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Gets all active shared folders for a specific device.
        /// REASON: ActionType 4 in proc_shared_folder_manager.
        /// </summary>
        public static List<SharedFolderModel> GetDeviceFolders(long userId, string deviceGuid, out string status, out string msg)
        {
            status = "error";
            msg = "";
            var list = new List<SharedFolderModel>();

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 4),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetStringParameter("device_guid", deviceGuid),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        list.Add(new SharedFolderModel
                        {
                            folder_id = Convert.ToInt64(r["folder_id"]),
                            folder_guid = Guid.Parse(r["folder_guid"].ToString()),
                            device_id = Convert.ToInt32(r["device_id"]),
                            device_name = r["device_name"].ToString(),
                            device_guid = Guid.Parse(r["device_guid"].ToString()),
                            device_status = r["device_status"].ToString(),
                            folder_name = r["folder_name"].ToString(),
                            local_path = r["local_path"].ToString(),
                            description = r["description"] != DBNull.Value ? r["description"].ToString() : "",
                            is_active = Convert.ToBoolean(r["is_active"]),
                            created_date = Convert.ToDateTime(r["created_date"]),
                            modified_date = Convert.ToDateTime(r["modified_date"]),
                            permission_count = Convert.ToInt32(r["permission_count"])
                        });
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return list;
            }
        }

        /// <summary>
        /// Retrieves the user permissions matrix for a shared folder.
        /// REASON: ActionType 5 in proc_shared_folder_manager.
        /// </summary>
        public static List<FolderPermissionModel> GetFolderPermissions(long userId, long folderId, out string status, out string msg)
        {
            status = "error";
            msg = "";
            var list = new List<FolderPermissionModel>();

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 5),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetLongParameter("folder_id", folderId),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        list.Add(new FolderPermissionModel
                        {
                            permission_id = Convert.ToInt64(r["permission_id"]),
                            folder_id = folderId,
                            user_id = Convert.ToInt64(r["user_id"]),
                            user_display_name = r["user_display_name"].ToString(),
                            user_name = r["user_name"].ToString(),
                            email = r["email"] != DBNull.Value ? r["email"].ToString() : "",
                            can_view = Convert.ToBoolean(r["can_view"]),
                            can_download = Convert.ToBoolean(r["can_download"]),
                            can_upload = Convert.ToBoolean(r["can_upload"]),
                            can_delete = Convert.ToBoolean(r["can_delete"]),
                            is_owner = Convert.ToBoolean(r["is_owner"])
                        });
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return list;
            }
        }

        /// <summary>
        /// Upserts permissions for a user on a shared folder.
        /// REASON: ActionType 6 in proc_shared_folder_manager.
        /// </summary>
        public static bool UpdatePermission(long userId, long folderId, long targetUserId, bool canView, bool canDownload, bool canUpload, bool canDelete, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 6),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetLongParameter("folder_id", folderId),
                    Parameters.GetLongParameter("target_user_id", targetUserId),
                    Parameters.GetBooleanParameter("can_view", canView),
                    Parameters.GetBooleanParameter("can_download", canDownload),
                    Parameters.GetBooleanParameter("can_upload", canUpload),
                    Parameters.GetBooleanParameter("can_delete", canDelete),
                    paramStatus,
                    paramMsg
                };

                MySqlHelper.ExecuteNonQuery("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                return status == "success";
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Retrieves all active users for team permission assignments.
        /// REASON: ActionType 8 in proc_shared_folder_manager.
        /// </summary>
        public static DataTable GetTeamUsers(out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 8),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                return dt;
            }
            catch (Exception ex)
            {
                status = "error";
                msg = ex.Message;
                return null;
            }
        }
/// <summary>
        /// WHAT: Verifies whether the requesting user has permission to browse this shared folder.
        /// REASON: Step 4 Remote File Browser - ActionType 9 in proc_shared_folder_manager.
        /// Strictly checks device ownership OR explicit CanView permission.
        /// </summary>
        public static FolderBrowseViewModel VerifyFolderViewPermission(long userId, Guid folderGuid, out string status, out string msg)
        {
            status = "error";
            msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 9),
                    Parameters.GetLongParameter("user_id", userId),
                    Parameters.GetStringParameter("folder_guid", folderGuid.ToString()),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (status == "success" && dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new FolderBrowseViewModel
                    {
                        folder_id = Convert.ToInt64(row["folder_id"]),
                        folder_guid = Guid.Parse(row["folder_guid"].ToString()),
                        folder_name = row["folder_name"].ToString(),
                        local_path = row["local_path"].ToString(),
                        description = row["description"] != DBNull.Value ? row["description"].ToString() : "",
                        device_id = Convert.ToInt32(row["device_id"]),
                        device_guid = Guid.Parse(row["device_guid"].ToString()),
                        device_name = row["device_name"].ToString(),
                        device_status = row["device_status"].ToString(),
                        is_device_online = Convert.ToInt32(row["is_device_online"]) == 1,
                        is_owner = Convert.ToBoolean(row["is_owner"]),
                        can_view = Convert.ToBoolean(row["can_view"]),
                        can_download = Convert.ToBoolean(row["can_download"]),
                        can_upload = Convert.ToBoolean(row["can_upload"]),
                        can_delete = Convert.ToBoolean(row["can_delete"])
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
        /// Retrieves all active shared folders across all devices belonging to the user.
        /// WHAT: Powers the dynamic Shared Folders dashboard card, device table counts, and list.
        /// REASON: Uses ActionType 10 in proc_shared_folder_manager to avoid raw SQL detection issues
        ///         that caused silent failures when CommandType detection misidentified the query.
        /// </summary>
        public static List<SharedFolderModel> GetAllUserSharedFolders(long userId)
        {
            var list = new List<SharedFolderModel>();
            string status = "error";
            string msg = "";

            try
            {
                SqlParameter paramStatus = new SqlParameter("@status", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                SqlParameter paramMsg = new SqlParameter("@msg", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    Parameters.GetIntParameter("ActionType", 10),
                    Parameters.GetLongParameter("user_id", userId),
                    paramStatus,
                    paramMsg
                };

                DataTable dt = MySqlHelper.ExecuteDataTable("proc_shared_folder_manager", parameters);
                status = paramStatus.Value != null ? paramStatus.Value.ToString() : "error";
                msg = paramMsg.Value != null ? paramMsg.Value.ToString() : "";

                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        list.Add(new SharedFolderModel
                        {
                            folder_id = Convert.ToInt64(r["folder_id"]),
                            folder_guid = Guid.Parse(r["folder_guid"].ToString()),
                            device_id = Convert.ToInt32(r["device_id"]),
                            device_name = r["device_name"].ToString(),
                            device_guid = Guid.Parse(r["device_guid"].ToString()),
                            device_status = r["device_status"].ToString(),
                            folder_name = r["folder_name"].ToString(),
                            local_path = r["local_path"].ToString(),
                            description = r["description"] != DBNull.Value ? r["description"].ToString() : "",
                            is_active = Convert.ToBoolean(r["is_active"]),
                            created_date = Convert.ToDateTime(r["created_date"]),
                            modified_date = Convert.ToDateTime(r["modified_date"]),
                            permission_count = Convert.ToInt32(r["permission_count"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback returns empty list; log ex.Message for diagnostics
                System.Diagnostics.Trace.TraceError("[GetAllUserSharedFolders] Exception: " + ex.Message);
            }

            return list;
        }
    }
}
