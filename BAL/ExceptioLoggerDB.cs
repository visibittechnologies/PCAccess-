using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using PCAccess.DAL;
using PCAccess.Models;

namespace PCAccess.BAL
{
    public class ExceptioLoggerDB
    {
        public static void SaveErrorException<TModel>(Exception ee, string Controller, TModel model)
        {
            //string Errorjson = "";
            //model = JsonConvert.DeserializeObject<AddUserDetails>(Errorjson);
            try
            {
                string modelJson = model != null ? JsonConvert.SerializeObject(model) : "";
                ExceptionLoggerModel logger = new ExceptionLoggerModel()
                {
                    ExceptionMessage = ee.Message,
                    ExceptionStackTrace = ee.StackTrace,
                    ControllerName = Controller,
                    LogTime = DateTime.Now,
                    modelJson = modelJson
                };
                SaveErrorLog(1, logger);
            }
            catch { }
        }
        public static void SaveErrorException(Exception ee, string Controller)
        {
            try
            {
                string modelJson = "";
                ExceptionLoggerModel logger = new ExceptionLoggerModel()
                {
                    ExceptionMessage = ee.Message,
                    ExceptionStackTrace = ee.StackTrace,
                    ControllerName = Controller,
                    LogTime = DateTime.Now,
                    modelJson = modelJson
                };
                SaveErrorLog(1, logger);
            }
            catch { }
        }
        public static DataTable SaveErrorLog(int flag, ExceptionLoggerModel objModel)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                {
                  new SqlParameter("@flag",flag),
                  new SqlParameter("@ExceptionMessage",objModel.ExceptionMessage != null ? objModel.ExceptionMessage : ""),
                  new SqlParameter("@ControllerName",objModel.ControllerName != null ? objModel.ControllerName : ""),
                  new SqlParameter("@ExceptionStackTrace",objModel.ExceptionStackTrace != null ? objModel.ExceptionStackTrace : ""),
                  new SqlParameter("@modelJson",objModel.modelJson != null ? objModel.modelJson : "")
                };
                dt = MySqlHelper.ExecuteDataTable("proc_saveErrorLog", parameters);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            return dt;
        }
    }
}