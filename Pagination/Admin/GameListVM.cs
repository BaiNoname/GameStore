using GameStore.Models;

namespace GameStore.Pagination.Admin
{
    // ViewModel để hiển thị danh sách game trong trang quản trị, hỗ trợ phân trang và lọc
    public class GameListVM
    {
        // Danh sách game hiển thị trên trang, đã được phân trang và lọc theo tiêu chí
        public List<Game> Games { get; set; } = new();

        // Trang hiện tại
        public int CurrentPage { get; set; }
        // Tổng số trang
        public int TotalPages { get; set; }

        // Từ khóa tìm kiếm
        public string? Keyword { get; set; }
        // ID của thể loại game
        public string? CategoryId { get; set; }

        // Danh sách các thể loại game
        public List<TheLoaiGame> Categories { get; set; } = new();
    }
}