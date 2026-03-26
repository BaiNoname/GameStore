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

        public bool AddOrUpdate(int userId, string gameId, int rating, string comment)
        {
            // ❌ chưa mua game -> cấm
            bool bought = db.ChiTietGiaoDiches
                .Any(x => x.GiaoDich.MaNguoiDung == userId && x.MaGame == gameId);

            if (!bought) return false;

            var existing = db.DanhGias
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == gameId);

            if (existing != null)
            {
                // 🔁 update
                existing.MucDiem = rating;
                existing.NhanXet = comment;
                existing.NgayDanhGia = DateTime.UtcNow;
            }
            else
            {
                // ➕ create
                db.DanhGias.Add(new DanhGia
                {
                    MaDG = Guid.NewGuid().ToString(),
                    MaNguoiDung = userId,
                    MaGame = gameId,
                    MucDiem = rating,
                    NhanXet = comment,
                    NgayDanhGia = DateTime.UtcNow
                });
            }

            return db.SaveChanges() > 0;
        }

        public DanhGia? GetUserReview(int userId, string gameId)
        {
            return db.DanhGias
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == gameId);
        }

        public List<DanhGia> GetByGame(string gameId)
        {
            return db.DanhGias
    .Include(x => x.NguoiDung)
    .Where(x => x.MaGame == gameId)
    .OrderByDescending(x => x.NgayDanhGia)
    .ToList();
        }
    }
}
