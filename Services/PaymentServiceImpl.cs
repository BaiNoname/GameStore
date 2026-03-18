using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class PaymentServiceImpl : PaymentService
    {
        private GameStoreContext db;

        public PaymentServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public List<GiaoDich> findAll()
        {
            return db.GiaoDiches
                     .Include(g => g.NguoiDung)
                     .OrderByDescending(g => g.NgayMua)
                     .ToList();
        }

        public List<GiaoDich> findAll(string keyword, string status, int page, int pageSize, out int totalPages)
        {
            var query = db.GiaoDiches
                          .Include(g => g.NguoiDung)
                          .AsQueryable();

            // 🔍 search theo email
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(g => g.NguoiDung.Email.ToLower().Contains(keyword));
            }

            // 🎯 filter trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(g => g.TrangThai == status);
            }

            int totalItems = query.Count();

            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderByDescending(g => g.NgayMua)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public GiaoDich findById(string id)
        {
            return db.GiaoDiches
                     .FirstOrDefault(g => g.MaGD == id);
        }

        public bool UpdateStatus(string id, string status)
        {
            try
            {
                var gd = db.GiaoDiches.Find(id);

                if (gd == null) return false;

                // chỉ cho phép status hợp lệ
                if (status != "Success" && status != "Failed")
                    return false;

                gd.TrangThai = status;

                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        // user page 
        public bool Checkout(int userId)
        {
            using var transaction = db.Database.BeginTransaction();

            try
            {
                var cart = db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                    .FirstOrDefault(g => g.MaNguoiDung == userId);

                if (cart == null || !cart.ChiTietGioHangs.Any())
                    return false;

                var giaoDich = new GiaoDich
                {
                    MaGD = Guid.NewGuid().ToString(),
                    MaNguoiDung = userId,
                    NgayMua = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "Success",
                    PhuongThuc = "Balance"
                };

                db.GiaoDiches.Add(giaoDich);

                decimal total = 0;

                foreach (var item in cart.ChiTietGioHangs)
                {
                    db.ChiTietGiaoDiches.Add(new ChiTietGiaoDich
                    {
                        MaGD = giaoDich.MaGD,
                        MaGame = item.MaGame,
                        DonGia = item.DonGiaHienTai
                    });

                    total += item.DonGiaHienTai;
                }

                giaoDich.ThanhTien = total;

                // clear cart
                db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);

                db.SaveChanges();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
    }
}
