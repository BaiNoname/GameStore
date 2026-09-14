using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class ReviewServiceImpl : ReviewService
    {
        private GameStoreContext db;

        public ReviewServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Thêm mới hoặc cập nhật đánh giá của người dùng cho một game
        public string AddOrUpdate(int userId, string gameId, int rating, string comment)
        {
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
            if (user == null) return "inactive_user";

            // Kiểm tra xem người dùng đã mua game chưa
            bool bought = db.ChiTietGiaoDiches
                .Any(x => x.GiaoDich.MaNguoiDung == userId && x.MaGame == gameId);

            // Nếu chưa mua, không cho phép đánh giá
            if (!bought) return "not_bought";

            // Kiểm tra xem người dùng đã đánh giá game này chưa
            var existing = db.DanhGias
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == gameId);

            // Nếu đã đánh giá, cập nhật lại điểm và nhận xét
            if (existing != null)
            {
                existing.MucDiem = rating;
                existing.NhanXet = comment;
                existing.NgayDanhGia = DateTime.UtcNow;

                db.SaveChanges();
                return "updated";
            }
            else
            {
                // Nếu chưa đánh giá, tạo mới một đánh giá
                db.DanhGias.Add(new DanhGia
                {
                    MaDG = Guid.NewGuid().ToString(),
                    MaNguoiDung = userId,
                    MaGame = gameId,
                    MucDiem = rating,
                    NhanXet = comment,
                    NgayDanhGia = DateTime.UtcNow
                });

                db.SaveChanges();
                return "created";
            }
        }

        // Lấy đánh giá của người dùng cho một game cụ thể
        public DanhGia? GetUserReview(int userId, string gameId)
        {
            // Lấy đánh giá của người dùng cho game, nếu không có thì trả về null
            return db.DanhGias
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == gameId);
        }
        
        // Lấy tất cả đánh giá của một game
        public List<DanhGia> GetByGame(string gameId)
        {
            // Lấy tất cả đánh giá của game, bao gồm thông tin người dùng, sắp xếp theo ngày đánh giá giảm dần
            return db.DanhGias
                .Include(x => x.NguoiDung)
                .Where(x => x.MaGame == gameId)
                .OrderByDescending(x => x.NgayDanhGia)
                .ToList();
        }
    }
}
