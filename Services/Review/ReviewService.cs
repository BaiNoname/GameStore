using GameStore.Models;

namespace GameStore.Services
{
    public interface ReviewService
    {
        // Thêm hoặc cập nhật đánh giá của người dùng cho một game
        public string AddOrUpdate(int userId, string gameId, int rating, string comment);
        // Lấy đánh giá của người dùng cho một game
        DanhGia? GetUserReview(int userId, string gameId);
        // Lấy tất cả đánh giá của một game
        List<DanhGia> GetByGame(string gameId);
    }
}
