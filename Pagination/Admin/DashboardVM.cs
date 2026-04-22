namespace GameStore.Pagination.Admin
{
    public class DashboardVM
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }

        public int SuccessOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AvgOrderValue { get; set; }

        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int LiveEvents { get; set; }
        public int EndedEvents { get; set; }

        public List<RevenueByDay> RevenueChart { get; set; } = new();
        public List<TopGame> TopGames { get; set; } = new();
        public List<RecentOrder> RecentOrders { get; set; } = new();
        public List<EventSummaryVM> RecentEvents { get; set; } = new();
        public List<EventSummaryVM> TopJoinedEvents { get; set; } = new();
    }

    public class RevenueByDay
    {
        public string Date { get; set; } = "";
        public decimal Revenue { get; set; }
    }

    public class TopGame
    {
        public string TenGame { get; set; } = "";
        public int SoLuotTai { get; set; }
    }

    public class RecentOrder
    {
        public string MaGD { get; set; } = "";
        public string Email { get; set; } = "";
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; } = "";
        public DateTime NgayMua { get; set; }
    }

    public class EventSummaryVM
    {
        public int EventId { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public string EventType { get; set; } = "";
        public int CurrentParticipants { get; set; }
        public int? MaxParticipants { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
    }
}