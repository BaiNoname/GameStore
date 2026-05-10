using GameStore.Models;

namespace GameStore.Pagination.Admin
{
    // ViewModel để hiển thị danh sách thể loại game với phân trang trong trang quản trị
    public class CategoryListVM
    {
        // Danh sách thể loại game hiện tại sau khi áp dụng các bộ lọc và phân trang
        public List<TheLoaiGame> Categories { get; set; } = new();
        // Trang hiện tại đang hiển thị
        public int CurrentPage { get; set; }
        // Tổng số trang dựa trên tổng số lượng thể loại game và số lượng thể loại game hiển thị trên mỗi trang
        public int TotalPages { get; set; }
        // Từ khóa tìm kiếm (nếu có) để hiển thị lại trong ô tìm kiếm sau khi người dùng thực hiện tìm kiếm
        public string? Keyword { get; set; }
    }
}