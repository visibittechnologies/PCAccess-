using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using PCAccess.DAL;
using PCAccess.MyFun;
using PCAccess.Models;
using static PCAccess.MyFun.OperatorModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCAccess.BAL
{
    public static class Profle
    {
      
    
        public static DataTable GetUserDetailsForEdit(string flag, long user_id)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                {
                new SqlParameter("@flag",flag),
                new SqlParameter("@user_id",user_id)
            };
                dt = MySqlHelper.ExecuteDataTable("edit_user_profile", parameters);
            }
            catch { }
            return dt;
        }
        public static DataTable UpdateProfile(string flag, AddUserDetails model, long created_by)
        {
            var dt = new DataTable();

            try
            {
                SqlParameter[] parameters =
                {
            new SqlParameter("@flag", flag),

            new SqlParameter("@user_id", model.user_id > 0 ? (object)model.user_id : DBNull.Value),

            new SqlParameter("@username",
                string.IsNullOrEmpty(model.username) ? (object)DBNull.Value : model.username),

            new SqlParameter("@password",
                string.IsNullOrEmpty(model.password) ? (object)DBNull.Value : model.password),

            new SqlParameter("@name",
                string.IsNullOrEmpty(model.name) ? (object)DBNull.Value : model.name),

            new SqlParameter("@email",
                string.IsNullOrEmpty(model.email) ? (object)DBNull.Value : model.email),

            new SqlParameter("@profile_photo",
                string.IsNullOrEmpty(model.profile_photo) ? (object)DBNull.Value : model.profile_photo),

            new SqlParameter("@created_by", created_by)
        };

                dt = MySqlHelper.ExecuteDataTable("edit_user_profile", parameters);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }

            return dt;
        }

    }
}