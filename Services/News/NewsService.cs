using GameStore.Models;

namespace GameStore.Services
{
    public interface NewsService
    {
        // Tìm kiếm tin tức với các tiêu chí: từ khóa, loại tin tức, trạng thái, phân trang
        List<News> FindAll(string keyword, string newsType, string status, int page, int pageSize, out int totalPages);
        // Tìm kiếm tin tức đã xuất bản với loại tin tức và phân trang
        List<News> FindPublished(string newsType, int page, int pageSize, out int totalPages);
        // Lấy các tin tức nổi bật
        List<News> GetFeatured(int take = 1);
        // Lấy các tin tức đang thịnh hành
        List<News> GetTrending(int take = 4);
        // Lấy các tin tức mới nhất
        List<News> GetLatest(int take = 6);
        // Tìm kiếm tin tức theo ID
        News? FindById(int id);
        // Tìm kiếm tin tức theo slug
        News? FindBySlug(string slug);
        // Tạo tin tức mới
        bool Create(News news);
        // Cập nhật tin tức
        bool Update(News news);
        // Xóa tin tức
        bool Delete(int id);
        // Tăng số lượt xem của tin tức
        bool IncreaseView(int id);
        // Hết hạn các tin tức cũ
        void ExpireOldNews();
    }
}