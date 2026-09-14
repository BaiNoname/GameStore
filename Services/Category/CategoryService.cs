using GameStore.Models;

namespace GameStore.Services
{
    public interface CategoryService
    {
        // Lấy tất cả danh mục game
        public List<TheLoaiGame> findAll();
        
        // Lấy danh mục game theo ID
        public TheLoaiGame findById(string id);
        // Tạo mới danh mục game
        public bool Create(TheLoaiGame category);
        // Cập nhật danh mục game
        public bool Update(TheLoaiGame category);
        // Xóa danh mục game
        public bool Delete(string id);
        // Tìm kiếm danh mục game theo từ khóa với phân trang
        public List<TheLoaiGame> findAll(string keyword, int page, int pageSize, out int totalPages);

    }
}
