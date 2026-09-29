namespace PCAccess.BAL
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Data;
    using System.Linq;
    using System.Web;
    using PCAccess.DAL;
    using PCAccess.MyFun;
    using PCAccess.Models;
    using static PCAccess.MyFun.OperatorModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    public class Company
    {
        public static class DataRowMapper
        {
            public static T MapToModel<T>(DataRow row) where T : new()
            {
                T obj = new T();

                foreach (var prop in typeof(T).GetProperties())
                {
                    if (row.Table.Columns.Contains(prop.Name))
                    {
                        var value = row[prop.Name];
                        if (value != DBNull.Value)
                        {
                            prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                }

                return obj;
            }
        }
        #region Company Details
        public static DataTable addCompanyProfile(string flag, long id, long login_id, string json_data)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                  {
            new SqlParameter("@flag",flag),
            new SqlParameter("@id",id),
            new SqlParameter("@user_id",login_id),
            new SqlParameter("@json_data",json_data)
        };
                dt = MySqlHelper.ExecuteDataTable("add_company_details", parameters);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            return dt;
        }
        public static DataTable GetCompanyData(string flag = "get", long user_id = 1, int id = 0)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@user_id", user_id),
                    new SqlParameter("@id", id > 0 ? (object)id : DBNull.Value)
                };
                dt = MySqlHelper.ExecuteDataTable("add_company_details", parameters);
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:GetCompanyData", flag);
            }
            return dt;
        }
        #endregion

        #region Meta Tags
        public static DataTable addPageMetaTag(string flag, SeoMetaModel model)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                  {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@id", model.ID > 0 ? model.ID : (object)DBNull.Value),
                    new SqlParameter("@user_id", model.user_id > 0 ? model.user_id : 0),
                    new SqlParameter("@page_name", string.IsNullOrEmpty(model.page_name) ? (object)DBNull.Value : model.page_name),
                    new SqlParameter("@page_url", string.IsNullOrEmpty(model.page_url) ? (object)DBNull.Value : model.page_url),
                    new SqlParameter("@meta_title", string.IsNullOrEmpty(model.meta_title) ? (object)DBNull.Value : model.meta_title),
                    new SqlParameter("@meta_keywords", string.IsNullOrEmpty(model.meta_keywords) ? (object)DBNull.Value : model.meta_keywords),
                    new SqlParameter("@meta_description", string.IsNullOrEmpty(model.meta_description) ? (object)DBNull.Value : model.meta_description),
                };
                dt = MySqlHelper.ExecuteDataTable("proc_add_page_meta_tags", parameters);

                if (parameters[1].Value != DBNull.Value)
                {
                    model.ID = Convert.ToInt32(parameters[1].Value);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            return dt;
        }
        #endregion

        #region Google Scripts
        public static DataTable addGoogleScript(string flag, SeoMetaModel model)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                  {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@id", model.ID > 0 ? model.ID : (object)DBNull.Value),
                    new SqlParameter("@user_id", model.user_id > 0 ? model.user_id : 0),
                    new SqlParameter("@googleScript", string.IsNullOrEmpty(model.google_script) ? (object)DBNull.Value : model.google_script),
                };
                dt = MySqlHelper.ExecuteDataTable("proc_add_seo_tool", parameters);
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:addGoogleScript", flag);
            }
            return dt;
        }
        #endregion

        #region Blog Management
        public static DataTable addBlog(string flag, long id, long login_id, string json_data)
        {
            var dt = new DataTable();
            try
            {
                JObject jsonDataObj = JObject.Parse(json_data);
                SqlParameter[] parameters = {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", login_id),
                    new SqlParameter("@title", (string)jsonDataObj["title"]),
                    new SqlParameter("@category", (string)jsonDataObj["category"]),
                    new SqlParameter("@author", (string)jsonDataObj["author"]),
                    new SqlParameter("@status", (string)jsonDataObj["status"]),
                    new SqlParameter("@blog_img", (string)jsonDataObj["blog_img"]),
                    new SqlParameter("@short_description", (string)jsonDataObj["short_description"]),
                    new SqlParameter("@long_description", (string)jsonDataObj["long_description"]),
                    new SqlParameter("@meta_title", (string)jsonDataObj["meta_title"]),
                    new SqlParameter("@meta_description", (string)jsonDataObj["meta_description"]),
                    new SqlParameter("@meta_keywords", (string)jsonDataObj["meta_keywords"]),
                    new SqlParameter("@scheduled_date", (string)jsonDataObj["scheduled_date"]),
                    new SqlParameter("@blog_type", (string)jsonDataObj["blog_type"]),
                    new SqlParameter("@crop_name", (string)jsonDataObj["crop_name"] ?? (object)DBNull.Value),
                    new SqlParameter("@mandi_name", (string)jsonDataObj["mandi_name"] ?? (object)DBNull.Value),
                    new SqlParameter("@price", (string)jsonDataObj["price"] ?? (object)DBNull.Value),
                    new SqlParameter("@price_change", (string)jsonDataObj["price_change"] ?? (object)DBNull.Value),
                    new SqlParameter("@location", (string)jsonDataObj["location"] ?? (object)DBNull.Value)
                };
                dt = MySqlHelper.ExecuteDataTable("add_blog", parameters);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            return dt;
        }
        public static DataTable GetBlogList(long user_id, string search, string sortcolname, string sortdir, int pageno, int pagesize, out int totalrows, string flag = "list")
        {
            var dt = new DataTable();
            try
            {
                totalrows = 0;
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag",flag),
                    user_id > 0 ?  new SqlParameter("@user_id", user_id) :  new SqlParameter("@user_id", DBNull.Value),
                    !string.IsNullOrEmpty(search) ? new SqlParameter("@search", search) :  new SqlParameter("@search", DBNull.Value),
                    new SqlParameter("@sortcol",sortcolname),
                    new SqlParameter("@sortdir",sortdir),
                    new SqlParameter("@page_no",pageno),
                    new SqlParameter("@page_size",pagesize)
                };

                dt = MySqlHelper.ExecuteDataTable("add_blog", parameters);
                if (dt.Rows.Count > 0) totalrows = Convert.ToInt32(dt.Rows[0]["overall_count"].ToString());
            }
            catch (SqlException ex)
            { throw ex; }
            return dt;
        }
        public static DataTable GetPublicBlogList(string blog_type, string category, string search, int pageno, int pagesize, out int totalrows)
        {
            var dt = new DataTable();
            totalrows = 0;
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@flag", "public_list"),
                    !string.IsNullOrEmpty(search) ? new SqlParameter("@search", search) : new SqlParameter("@search", DBNull.Value),
                    !string.IsNullOrEmpty(blog_type) ? new SqlParameter("@blog_type", blog_type) : new SqlParameter("@blog_type", DBNull.Value),
                    !string.IsNullOrEmpty(category) ? new SqlParameter("@category", category) : new SqlParameter("@category", DBNull.Value),
                    new SqlParameter("@page_no", pageno),
                    new SqlParameter("@page_size", pagesize)
                };

                dt = MySqlHelper.ExecuteDataTable("add_blog", parameters);
                if (dt.Rows.Count > 0) totalrows = Convert.ToInt32(dt.Rows[0]["overall_count"].ToString());
            }
            catch (SqlException ex)
            { throw ex; }
            return dt;
        }
        public static DataTable GetBlogDetails(long id, long user_id, string flag = "getblog")
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@flag",flag),
                    new SqlParameter("@id",id),
                    new SqlParameter("@user_id",user_id),
                };
                dt = MySqlHelper.ExecuteDataTable("add_blog", parameters);
            }
            catch { }
            return dt;
        }
       public static DataTable GetBlogKPI()
        {
            SqlParameter[] parameters = { new SqlParameter("@flag", "kpi") };
            return MySqlHelper.ExecuteDataTable("add_blog", parameters);
        }

        public static DataTable getBlog(string flag)
        {
            SqlParameter[] parameters = { new SqlParameter("@flag", flag) };
            return MySqlHelper.ExecuteDataTable("add_blog", parameters);
        }

        public static DataTable getBlogMetaDetails(string flag, long id)
        {
            SqlParameter[] parameters = { 
                new SqlParameter("@flag", flag),
                new SqlParameter("@id", id)
            };
            return MySqlHelper.ExecuteDataTable("add_blog", parameters);
        }
        #endregion
    
        #region Category Management
        public static DataTable addCategory(string flag, long id, long login_id, string json_data)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", login_id),
                    new SqlParameter("@json_data", json_data)
                };
                dt = MySqlHelper.ExecuteDataTable("proc_category_master", parameters);
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:addCategory", flag);
            }
            return dt;
        }

        public static DataTable GetCategoryList(long user_id, string search, string sortcolname, string sortdir, int pageno, int pagesize, out int totalrows, string flag = "list")
        {
            var dt = new DataTable();
            totalrows = 0;
            try
            {
                SqlParameter[] parameters =
                {
                    Parameters.GetStringParameter("flag", flag),
                    user_id > 0 ? new SqlParameter("@user_id", user_id) : new SqlParameter("@user_id", DBNull.Value),
                    !string.IsNullOrEmpty(search) ? new SqlParameter("@search", search) : new SqlParameter("@search", DBNull.Value),
                    new SqlParameter("@sortcol", sortcolname),
                    new SqlParameter("@sortdir", sortdir),
                    new SqlParameter("@page_no", pageno),
                    new SqlParameter("@page_size", pagesize)
                };

                dt = MySqlHelper.ExecuteDataTable("proc_category_master", parameters);
                if (dt.Rows.Count > 0)
                {
                    totalrows = Convert.ToInt32(dt.Rows[0]["overall_count"].ToString());
                }
            }
            catch (SqlException ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:GetCategoryList", flag);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetCategoryDetails(long id, long user_id, string flag = "get")
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@flag", flag),
                    new SqlParameter("@id", id),
                    new SqlParameter("@user_id", user_id)
                };
                dt = MySqlHelper.ExecuteDataTable("proc_category_master", parameters);
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:GetCategoryDetails", flag);
            }
            return dt;
        }
        #endregion

        #region Page Meta & Active Scripts Helpers
        public static SeoMetaModel GetPageMeta(string absolutePath)
        {
            var model = new SeoMetaModel();
            try
            {
                var dt = addPageMetaTag("view", new SeoMetaModel());
                if (dt != null && dt.Rows.Count > 0)
                {
                    string normCurrent = (absolutePath ?? "").Trim('/', ' ').ToLower();
                    bool isHome = normCurrent == "" || normCurrent == "home" || normCurrent == "home/index";

                    foreach (DataRow row in dt.Rows)
                    {
                        string dbUrl = row["page_url"]?.ToString() ?? "";
                        string normDb = dbUrl;
                        if (dbUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || dbUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            try { normDb = new Uri(dbUrl).AbsolutePath; } catch {}
                        }
                        normDb = normDb.Trim('/', ' ').ToLower();

                        if (normDb == normCurrent || (isHome && (normDb == "" || normDb == "home" || normDb == "home/index")))
                        {
                            model.meta_title = row["meta_title"]?.ToString();
                            model.meta_keywords = row["meta_keywords"]?.ToString();
                            model.meta_description = row["meta_description"]?.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:GetPageMeta", absolutePath);
            }
            return model;
        }

        public static string GetActiveScripts()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                var dt = addGoogleScript("view", new SeoMetaModel());
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        sb.AppendLine(row["ScriptContent"]?.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptioLoggerDB.SaveErrorException(ex, "Company:GetActiveScripts", "");
            }
            return sb.ToString();
        }
        #endregion
        public static DataTable AddSubscriber(string email, string ipAddress = null, string createdBy = null)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters = {
                    new SqlParameter("@Email", email),
                    new SqlParameter("@IPAddress", (object)ipAddress ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object)createdBy ?? DBNull.Value)
                };
                dt = MySqlHelper.ExecuteDataTable("add_subscriber", parameters);
            }
            catch { }
            return dt;
        }

        public static DataTable GetSubscribersList(JqDataTableModel model)
        {
            var dt = new DataTable();
            try
            {
                SqlParameter[] parameters = {
                    new SqlParameter("@lValue1", model.lValue1),
                    new SqlParameter("@lValue2", model.lValue2),
                    new SqlParameter("@iDisplayStart", model.iDisplayStart),
                    new SqlParameter("@iDisplayLength", model.iDisplayLength),
                    new SqlParameter("@sSearch", model.sSearch ?? ""),
                    new SqlParameter("@sSortColName", model.sSortColName ?? "Id"),
                    new SqlParameter("@sSortDir_0", model.sSortDir_0 ?? "desc")
                };
                dt = MySqlHelper.ExecuteDataTable("get_subscribers_list", parameters);
            }
            catch { }
            return dt;
        }
        public static DataTable UpdateBlogViewCount(long blogId)
        {
            var dt = new DataTable();
            dt.Columns.Add("view_count");
            try
            {
                SqlParameter[] getParams = { new SqlParameter("@blog_id", blogId) };
                var infoDt = MySqlHelper.ExecuteDataTable("SELECT short_description FROM tbl_blog WHERE blog_id = @blog_id", getParams);
                
                if (infoDt != null && infoDt.Rows.Count > 0)
                {
                    string desc = infoDt.Rows[0]["short_description"]?.ToString() ?? "";
                    string newDesc = IncrementStringNumber(desc);
                    
                    SqlParameter[] updParams = { 
                        new SqlParameter("@blog_id", blogId),
                        new SqlParameter("@desc", newDesc)
                    };
                    MySqlHelper.ExecuteNonQuery("UPDATE tbl_blog SET short_description = @desc WHERE blog_id = @blog_id", updParams);
                    
                    try
                    {
                        string trackingQuery = @"
                            IF EXISTS (SELECT 1 FROM tbl_blog_views_tracking WHERE blog_id = @blog_id AND view_date = CAST(GETDATE() AS DATE))
                            BEGIN
                                UPDATE tbl_blog_views_tracking SET views_count = views_count + 1 WHERE blog_id = @blog_id AND view_date = CAST(GETDATE() AS DATE);
                            END
                            ELSE
                            BEGIN
                                INSERT INTO tbl_blog_views_tracking (blog_id, view_date, views_count) VALUES (@blog_id, CAST(GETDATE() AS DATE), 1);
                            END";
                        MySqlHelper.ExecuteNonQuery(trackingQuery, new SqlParameter("@blog_id", blogId));
                    }
                    catch { }

                    dt.Rows.Add(newDesc);
                }
            }
            catch { }
            return dt;
        }

        private static string IncrementStringNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "1";

            var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d+(\.\d+)?)([KkMm]?)");
            if (match.Success)
            {
                string numPart = match.Groups[1].Value;
                string suffix = match.Groups[3].Value.ToUpper();
                
                if (double.TryParse(numPart, out double num))
                {
                    if (suffix == "K") num *= 1000;
                    else if (suffix == "M") num *= 1000000;
                    
                    num++; // Increment
                    
                    string formattedNum = num.ToString();
                    if (num >= 1000000 && num % 100000 == 0) // Keep M format if perfectly divisible
                        formattedNum = (num / 1000000.0).ToString("0.#") + "M";
                    else if (num >= 1000 && num % 100 == 0) // Keep K format if perfectly divisible
                        formattedNum = (num / 1000.0).ToString("0.#") + "K";
                    else
                        formattedNum = num.ToString();

                    return input.Substring(0, match.Index) + formattedNum + input.Substring(match.Index + match.Length);
                }
            }
            return input + " 1"; // Fallback if no number found
        }
    }
}
