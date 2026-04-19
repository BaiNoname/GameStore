using GameStore.Models;

namespace GameStore.Pagination.User
{
    public class NewsPageVM
    {
        public List<Models.News> FeaturedNews { get; set; } = new();
        public List<Models.News> LatestNews { get; set; } = new();
        public List<Models.News> TrendingNews { get; set; } = new();

        public string NewsType { get; set; } = "All";
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}