using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PCAccess.Models;

namespace PCAccess.BAL
{
    public class DashboardManager
    {
        public static AdminDashboardViewModel GetDashboardOverview()
        {
            var model = new AdminDashboardViewModel();
            try
            {
                using (var connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("sp_get_admin_dashboard_stats", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            // 1. Counts
                            if (reader.Read())
                            {
                                model.Counts.TotalBlogs = reader["TotalBlogs"] != DBNull.Value ? Convert.ToInt32(reader["TotalBlogs"]) : 0;
                                model.Counts.PublishedBlogs = reader["PublishedBlogs"] != DBNull.Value ? Convert.ToInt32(reader["PublishedBlogs"]) : 0;
                                model.Counts.DraftBlogs = reader["DraftBlogs"] != DBNull.Value ? Convert.ToInt32(reader["DraftBlogs"]) : 0;
                                model.Counts.TotalCategories = reader["TotalCategories"] != DBNull.Value ? Convert.ToInt32(reader["TotalCategories"]) : 0;
                                model.Counts.TotalVideos = reader["TotalVideos"] != DBNull.Value ? Convert.ToInt32(reader["TotalVideos"]) : 0;
                                model.Counts.TotalReels = reader["TotalReels"] != DBNull.Value ? Convert.ToInt32(reader["TotalReels"]) : 0;
                                model.Counts.TotalSubscribers = reader["TotalSubscribers"] != DBNull.Value ? Convert.ToInt32(reader["TotalSubscribers"]) : 0;
                                model.Counts.TotalComments = reader["TotalComments"] != DBNull.Value ? Convert.ToInt32(reader["TotalComments"]) : 0;
                            }

                            // 2. Recent Content
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    model.RecentContent.Add(new RecentContentModel
                                    {
                                        BlogId = reader["blog_id"] != DBNull.Value ? Convert.ToInt64(reader["blog_id"]) : 0,
                                        Title = reader["title"]?.ToString(),
                                        Category = reader["category"]?.ToString(),
                                        Author = reader["author"]?.ToString(),
                                        Status = reader["status"]?.ToString(),
                                        CreatedDate = reader["created_date"] != DBNull.Value ? Convert.ToDateTime(reader["created_date"]) : (DateTime?)null,
                                        ViewCount = reader["view_count"] != DBNull.Value ? Convert.ToInt64(reader["view_count"]) : 0,
                                        BlogType = reader["blog_type"]?.ToString()
                                    });
                                }
                            }

                            // 3. Monthly Blog Views (Last 6 Months)
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    model.MonthlyViews.Add(new MonthlyViewData
                                    {
                                        MonthName = reader["MonthName"]?.ToString(),
                                        ViewCount = reader["ViewCount"] != DBNull.Value ? Convert.ToInt64(reader["ViewCount"]) : 0
                                    });
                                }
                            }

                            // 4. Website Traffic
                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                {
                                    model.Traffic.TotalVisitors = reader["TotalVisitors"] != DBNull.Value ? Convert.ToInt64(reader["TotalVisitors"]) : 0;
                                    model.Traffic.DirectTraffic = reader["DirectTraffic"] != DBNull.Value ? Convert.ToInt64(reader["DirectTraffic"]) : 0;
                                    model.Traffic.SocialTraffic = reader["SocialTraffic"] != DBNull.Value ? Convert.ToInt64(reader["SocialTraffic"]) : 0;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception or ignore for now to return default model
            }
            return model;
        }
    }
}
