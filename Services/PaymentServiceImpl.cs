using GameStore.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GameStore.Services
{
    public class PaymentServiceImpl : PaymentService
    {
        private GameStoreContext db;
        private readonly ILogger<PaymentServiceImpl> logger;
        private readonly IHubContext<GameStore.Hubs.GameHub> hub;

        public PaymentServiceImpl(GameStoreContext _db, ILogger<PaymentServiceImpl> _logger, IHubContext<GameStore.Hubs.GameHub> _hub)
        {
            db = _db;
            logger = _logger;
            hub = _hub;
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
        public async Task<bool> Checkout(int userId)
        {
            using var transaction = db.Database.BeginTransaction();

            try
            {
                var cart = db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.Game)
                    .FirstOrDefault(g => g.MaNguoiDung == userId);

                if (cart == null || !cart.ChiTietGioHangs.Any())
                {
                    logger.LogInformation("Checkout: cart empty for user {UserId}", userId);
                    return false;
                }

                var user = db.NguoiDungs
                    .Where(u => u.MaNguoiDung == userId)
                    .FirstOrDefault();

                if (user == null)
                {
                    logger.LogWarning("Checkout: user not found {UserId}", userId);
                    return false;
                }

                // 🔥 reload lại balance từ DB (tránh cache)
                db.Entry(user).Reload();

                decimal total = cart.ChiTietGioHangs.Sum(x => x.DonGiaHienTai);

                logger.LogInformation("Checkout start: UserId={UserId}, BalanceBefore={BalanceBefore}, Total={Total}",
                    userId, user.SoDu, total);

                // ❌ không đủ tiền
                if (user.SoDu < total)
                {
                    logger.LogInformation("Checkout failed: insufficient balance. UserId={UserId}", userId);
                    return false;
                }

                // 🔥 CHECK đã mua game chưa
                var gameIds = cart.ChiTietGioHangs.Select(x => x.MaGame).ToList();

                var alreadyOwned = db.ChiTietGiaoDiches
                    .Where(ct => ct.GiaoDich.MaNguoiDung == userId && gameIds.Contains(ct.MaGame))
                    .Select(ct => ct.MaGame)
                    .ToList();

                if (alreadyOwned.Any())
                {
                    logger.LogWarning("User already owns some games: {Games}", string.Join(",", alreadyOwned));
                    return false;
                }

                // 🔥 tạo transaction trước (giống VNPAY)
                var giaoDich = new GiaoDich
                {
                    MaGD = Guid.NewGuid().ToString(),
                    MaNguoiDung = userId,
                    NgayMua = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "Pending", // 🔥 đổi từ Success → Pending
                    PhuongThuc = "Balance",
                    ThanhTien = total
                };

                db.GiaoDiches.Add(giaoDich);

                // 🔥 trừ tiền sau khi tạo transaction
                user.SoDu -= total;

                foreach (var item in cart.ChiTietGioHangs)
                {
                    db.ChiTietGiaoDiches.Add(new ChiTietGiaoDich
                    {
                        MaGD = giaoDich.MaGD,
                        MaGame = item.MaGame,
                        DonGia = item.DonGiaHienTai
                    });

                    var game = db.Games.Find(item.MaGame);
                    if (game != null)
                    {
                        game.SoLuotTai += 1;

                        // 🔥 PUSH REALTIME TO CLIENT
                        await hub.Clients.Group(game.MaGame.ToString())
                            .SendAsync("UpdateDownload", game.MaGame, game.SoLuotTai);
                        await hub.Clients.All
                            .SendAsync("UpdateDownload", game.MaGame, game.SoLuotTai);
                    }

                }

                // clear cart
                db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);

                // 🔥 set success sau cùng
                giaoDich.TrangThai = "Success";

                db.SaveChanges();
                transaction.Commit();

                logger.LogInformation("Checkout success: UserId={UserId}, BalanceAfter={BalanceAfter}",
                    userId, user.SoDu);

                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "Checkout exception for user {UserId}", userId);
                return false;
            }
        }

    }
}
