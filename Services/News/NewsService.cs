using GameStore.Models;

namespace GameStore.Services
{
    public interface NewsService
    {
        List<News> FindAll(string keyword, string newsType, string status, int page, int pageSize, out int totalPages);
        List<News> FindPublished(string newsType, int page, int pageSize, out int totalPages);
        List<News> GetFeatured(int take = 1);
        List<News> GetTrending(int take = 4);
        List<News> GetLatest(int take = 6);
        News? FindById(int id);
        News? FindBySlug(string slug);
        bool Create(News news);
        bool Update(News news);
        bool Delete(int id);
        bool IncreaseView(int id);
        void ExpireOldNews();
    }
}