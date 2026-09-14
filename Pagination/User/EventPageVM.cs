using GameStore.Models;

namespace GameStore.Pagination.User
{
    // ViewModel để truyền dữ liệu sự kiện đến view, bao gồm các danh sách sự kiện và thông tin phân trang
    public class EventPageVM
    {
        // Danh sách sự kiện nổi bật, sự kiện đang diễn ra, sự kiện sắp diễn ra và tất cả sự kiện
        public List<Event> FeaturedEvents { get; set; } = new();
        // Danh sách tất cả sự kiện, có thể được lọc theo loại sự kiện và trạng thái
        public List<Event> Events { get; set; } = new();
        // Danh sách sự kiện đang diễn ra
        public List<Event> LiveEvents { get; set; } = new();
        // Danh sách sự kiện sắp diễn ra
        public List<Event> UpcomingEvents { get; set; } = new();

        // Loại sự kiện hiện tại (All / Featured / Live / Upcoming)
        public string EventType { get; set; } = "All";
        // Trạng thái hiện tại (All / Active / Inactive)
        public string Status { get; set; } = "All";
        // Trang hiện tại
        public int CurrentPage { get; set; }
        // Tổng số trang
        public int TotalPages { get; set; }
    }
}