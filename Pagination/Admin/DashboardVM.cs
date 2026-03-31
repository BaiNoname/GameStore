namespace GameStore.Pagination.Admin
{
    public class DashboardVM
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }

        public List<RevenueByDay> RevenueChart { get; set; } = new();
        public List<TopGame> TopGames { get; set; } = new();
        public List<RecentOrder> RecentOrders { get; set; } = new();
    }

    public class RevenueByDay
    {
        public string Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopGame
    {
        public string TenGame { get; set; }
        public int SoLuotTai { get; set; }
    }

    public class RecentOrder
    {
        public string MaGD { get; set; }
        public string Email { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; }
    }
}
