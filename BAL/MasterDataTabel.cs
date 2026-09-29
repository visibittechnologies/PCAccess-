using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using PCAccess.DAL;
using PCAccess.Models;

namespace PCAccess.BAL
{
    public class MasterDataTabel
    {
        public static DataTable add_get_common_data(ApiParametersModel model, out string status, out string msg)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter Paramsg = new SqlParameter("@msg", "");
                Paramsg.Direction = ParameterDirection.Output; Paramsg.Size = 255;

                SqlParameter Parastatus = new SqlParameter("@status", "");
                Parastatus.Direction = ParameterDirection.Output; Parastatus.Size = 255;
                SqlParameter[] parameters =
               {
                  Parameters.GetStringParameter("flag",model.flag),
                  Parameters.GetStringParameter_Null("add_update_jsonData",model.add_update_jsonData),
                  Parameters.GetIntParameter("PageSize",model.PageSize>0?model.PageSize:20),
                  Parameters.GetIntParameter("PageNo",model.PageNo>0?model.PageNo:1),
                  Parameters.GetStringParameter_Null("SearchCriteria",model.searchcriteria),
                  Parameters.GetStringParameter_Null("filter_list",model.filter_list),
                  Paramsg,
                  Parastatus
                };
                dt = MySqlHelper.ExecuteDataTable("[dbo].[proc_add_get_common_data]", parameters);
                msg = (string)Paramsg.Value;
                status = (string)Parastatus.Value;
            }
            catch (SqlException ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "MasterDataTable:add_get_common_data", model);
                throw ex;
            }
            return dt;
        }
        public static DataTable CommonDelete(string flag, long id)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@Flag", flag),
                new SqlParameter("@Id", id)
            };
            return MySqlHelper.ExecuteDataTable("proc_project_remove", parameters);
        }
        public static DataSet GetAllDropDownValues(string type,string flag,int keyId)
        {
            var ds = new DataSet();
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@ReferenceType", type),
                    new SqlParameter("@keyId", keyId)
                };
                ds = MySqlHelper.ExecuteDataSet("proc_get_dropdown_value", parameters);
                if (ds.Tables.Count > 0)
                    ds.Tables[0].TableName = "dropdown";
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            return ds;
        }
    }
}