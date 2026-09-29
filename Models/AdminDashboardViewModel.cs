using System;
using System.Collections.Generic;

namespace PCAccess.Models
{
    public class AdminDashboardViewModel
    {
        public List<DeviceModel> Devices { get; set; } = new List<DeviceModel>();
        public List<SharedFolderModel> SharedFolders { get; set; } = new List<SharedFolderModel>();
        public int TotalSharedFolders { get; set; }
        public int OnlineDevicesCount { get; set; }
        public List<DashboardActivityItemModel> RecentActivities { get; set; } = new List<DashboardActivityItemModel>();
        public DashboardCounts Counts { get; set; } = new DashboardCounts();
        public List<RecentContentModel> RecentContent { get; set; } = new List<RecentContentModel>();
        public List<MonthlyViewData> MonthlyViews { get; set; } = new List<MonthlyViewData>();
        public TrafficData Traffic { get; set; } = new TrafficData();
    }

    public class DashboardActivityItemModel
    {
        public string Icon { get; set; } = "activity";
        public string Title { get; set; }
        public string Description { get; set; }
        public string TimeAgo { get; set; }
        public string BadgeColor { get; set; } = "emerald";
        public string LinkUrl { get; set; }
    }

    public class DashboardCounts
    {
        public int TotalBlogs { get; set; }
        public int PublishedBlogs { get; set; }
        public int DraftBlogs { get; set; }
        public int TotalCategories { get; set; }
        public int TotalVideos { get; set; }
        public int TotalReels { get; set; }
        public int TotalSubscribers { get; set; }
        public int TotalComments { get; set; }
    }

    public class RecentContentModel
    {
        public long BlogId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Author { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long ViewCount { get; set; }
        public string BlogType { get; set; }
    }

    public class MonthlyViewData
    {
        public string MonthName { get; set; }
        public long ViewCount { get; set; }
    }

    public class TrafficData
    {
        public long TotalVisitors { get; set; }
        public long DirectTraffic { get; set; }
        public long SocialTraffic { get; set; }
    }
}
