using GameStore.Models;

namespace GameStore.Pagination.User
{
    // ViewModel cho trang tin tức, chứa các danh sách tin tức được phân loại và thông tin phân trang
    public class NewsPageVM
    {
        // Danh sách tin tức nổi bật
        public List<Models.News> FeaturedNews { get; set; } = new();
        // Danh sách tin tức mới nhất
        public List<Models.News> LatestNews { get; set; } = new();
        // Danh sách tin tức đang thịnh hành
        public List<Models.News> TrendingNews { get; set; } = new();

        // Loại tin tức hiện tại (All / Featured / Latest / Trending)
        public string NewsType { get; set; } = "All";
        // Trang hiện tại
        public int CurrentPage { get; set; }
        // Tổng số trang
        public int TotalPages { get; set; }
    }
}