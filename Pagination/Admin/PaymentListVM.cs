using GameStore.Models;

namespace GameStore.Pagination.Admin
{
    // ViewModel cho trang quản lý giao dịch (payments) với phân trang và bộ lọc
    public class PaymentListVM
    {
        // Danh sách giao dịch hiển thị trên trang, có thể được lọc theo email và trạng thái
        public List<GiaoDich> Payments { get; set; } = new();

        // Trang hiện tại
        public int CurrentPage { get; set; }
        // Tổng số trang
        public int TotalPages { get; set; }

        // Từ khóa tìm kiếm (email)
        public string? Keyword { get; set; }
        // Trạng thái giao dịch (Success / Failed)
        public string? Status { get; set; }
    }
}