using GameStore.Models;

namespace GameStore.Pagination.Admin
{
    // ViewModel để hiển thị danh sách người dùng với phân trang trong trang quản trị
    public class UserListVM
    {
        // Danh sách người dùng được hiển thị trên trang, có thể được lọc theo từ khóa tìm kiếm
        public List<NguoiDung> Users { get; set; } = new();

        // Trang hiện tại
        public int CurrentPage { get; set; }
        // Tổng số trang
        public int TotalPages { get; set; }

        // Từ khóa tìm kiếm
        public string? Keyword { get; set; }
    }
}