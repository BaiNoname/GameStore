using GameStore.Models;

namespace GameStore.Services
{
    // 1 mục hiển thị trên thanh chạy ngang
    public class TickerItem
    {
        public string Icon { get; set; } = "📢";
        public string Text { get; set; } = "";
        public string? Link { get; set; }
    }

    public interface NotificationService
    {
        // Tổng hợp các mục cho thanh chạy ngang: thông báo admin + game mới + sự kiện sắp diễn ra + lượt mua gần đây
        List<TickerItem> GetTickerItems();

        // ----- Admin quản lý thông báo tự tạo (Custom) -----
        List<ThongBao> FindAll(string keyword, int page, int pageSize, out int totalPages);
        ThongBao? GetById(int maTB);
        (bool ok, string message) Create(ThongBao tb);
        (bool ok, string message) Update(ThongBao tb);
        (bool ok, string message) ToggleActive(int maTB);
        (bool ok, string message) Delete(int maTB);
    }
}
