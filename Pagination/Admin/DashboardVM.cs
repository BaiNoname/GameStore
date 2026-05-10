namespace GameStore.Pagination.Admin
{
    public class DashboardVM
    {
        // Các chỉ số tổng quan cho dashboard
        // Tổng doanh thu từ tất cả các đơn hàng
        public decimal TotalRevenue { get; set; }
        // Tổng số đơn hàng đã được tạo
        public int TotalOrders { get; set; }
        // Tổng số người dùng đã đăng ký
        public int TotalUsers { get; set; }
        // Tổng số game có trong hệ thống
        public int TotalGames { get; set; }

        // Tổng số đơn hàng thành công
        public int SuccessOrders { get; set; }
        // Tổng số đơn hàng đang chờ xử lý
        public int PendingOrders { get; set; }
        // Doanh thu trong ngày
        public decimal RevenueToday { get; set; }
        // Doanh thu trong tháng
        public decimal RevenueThisMonth { get; set; }
        // Giá trị trung bình của mỗi đơn hàng
        public decimal AvgOrderValue { get; set; }

        // Tổng số sự kiện
        public int TotalEvents { get; set; }
        // Tổng số sự kiện sắp diễn ra
        public int UpcomingEvents { get; set; }
        // Tổng số sự kiện đang diễn ra
        public int LiveEvents { get; set; }
        // Tổng số sự kiện đã kết thúc
        public int EndedEvents { get; set; }

        // Biểu đồ doanh thu theo ngày
        public List<RevenueByDay> RevenueChart { get; set; } = new();
        // Danh sách các game bán chạy nhất
        public List<TopGame> TopGames { get; set; } = new();
        // Danh sách các đơn hàng gần đây
        public List<RecentOrder> RecentOrders { get; set; } = new();
        // Danh sách các sự kiện gần đây
        public List<EventSummaryVM> RecentEvents { get; set; } = new();
        // Danh sách các sự kiện có số lượng người tham gia nhiều nhất
        public List<EventSummaryVM> TopJoinedEvents { get; set; } = new();
    }

    // Lớp để biểu diễn doanh thu theo ngày cho biểu đồ
    public class RevenueByDay
    {
        // Ngày (định dạng "yyyy-MM-dd")
        public string Date { get; set; } = "";
        // Doanh thu trong ngày
        public decimal Revenue { get; set; }
    }

    // Lớp để biểu diễn thông tin về game bán chạy nhất
    public class TopGame
    {
        // Tên game
        public string TenGame { get; set; } = "";
        // Số lượt tải của game
        public int SoLuotTai { get; set; }
    }

    // Lớp để biểu diễn thông tin về đơn hàng gần đây
    public class RecentOrder
    {
        // Mã giao dịch
        public string MaGD { get; set; } = "";
        // Email người dùng
        public string Email { get; set; } = "";
        // Thành tiền của đơn hàng
        public decimal ThanhTien { get; set; }
        // Trạng thái của đơn hàng
        public string TrangThai { get; set; } = "";
        // Ngày mua của đơn hàng
        public DateTime NgayMua { get; set; }
    }

    // Lớp để biểu diễn thông tin tóm tắt về sự kiện
    public class EventSummaryVM
    {
        // ID của sự kiện
        public int EventId { get; set; }
        // Tiêu đề của sự kiện
        public string Title { get; set; } = "";
        // Trạng thái của sự kiện
        public string Status { get; set; } = "";
        // Loại sự kiện
        public string EventType { get; set; } = "";
        // Số lượng người tham gia hiện tại
        public int CurrentParticipants { get; set; }
        // Số lượng người tham gia tối đa
        public int? MaxParticipants { get; set; }
        // Thời gian bắt đầu của sự kiện
        public DateTime StartAt { get; set; }
        // Thời gian kết thúc của sự kiện
        public DateTime EndAt { get; set; }
    }
}