using GameStore.Models;

namespace GameStore.Services
{
    public interface ReviewService
    {
        public string AddOrUpdate(int userId, string gameId, int rating, string comment);
        DanhGia? GetUserReview(int userId, string gameId);
        List<DanhGia> GetByGame(string gameId);
    }
}
