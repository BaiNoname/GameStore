using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GameStore.Services
{
    public class PaymentServiceImpl : PaymentService
    {
        //
        private GameStoreContext db;
        // Logger để ghi log thông tin, cảnh báo và lỗi
        private readonly ILogger<PaymentServiceImpl> logger;
        // HubContext để gửi thông báo realtime đến client khi có thay đổi về lượt tải game
        private readonly IHubContext<Hubs.GameHub> hub;
        // Service để xử lý logic liên quan đến tham gia sự kiện có trả phí
        private readonly EventParticipantService eventParticipantService;

        public PaymentServiceImpl(GameStoreContext _db, ILogger<PaymentServiceImpl> _logger, 
            IHubContext<Hubs.GameHub> _hub, EventParticipantService _eventParticipantService)
        {
            db = _db;
            logger = _logger;
            hub = _hub;
            eventParticipantService = _eventParticipantService;
        }

        // Kiểm tra và lấy thông tin người dùng nếu còn hoạt động
        private NguoiDung? GetActiveUser(int userId)
        {
            return db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
        }

        // tất cả giao dịch (admin page)
        public List<GiaoDich> findAll()
        {
            return db.GiaoDiches
                     .Include(g => g.NguoiDung)
                     .OrderByDescending(g => g.NgayMua)
                     .ToList();
        }

        // tìm kiếm và phân trang giao dịch (admin page)
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
        
        // tìm kiếm giao dịch theo ID (admin page)
        public GiaoDich findById(string id)
        {
            return db.GiaoDiches
                     .FirstOrDefault(g => g.MaGD == id);
        }

        // cập nhật trạng thái giao dịch (admin page)
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

        // user page thanh toán bằng balance trong tài khoản
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

                var user = GetActiveUser(userId);

                if (user == null)
                {
                    logger.LogWarning("Checkout: user not found {UserId}", userId);
                    return false;
                }

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

                var alreadyOwned = db.ThuVienGames
                    .Where(x => x.MaNguoiDung == userId && gameIds.Contains(x.MaGame))
                    .Select(x => x.MaGame)
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
                    NgayMua = DateTime.UtcNow,
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

                    // 🔥 ADD VÀO THƯ VIỆN
                    var exists = db.ThuVienGames
                        .Any(x => x.MaNguoiDung == userId && x.MaGame == item.MaGame);

                    if (!exists)
                    {
                        db.ThuVienGames.Add(new ThuVienGame
                        {
                            MaNguoiDung = userId,
                            MaGame = item.MaGame,
                            NgayMua = DateTime.UtcNow,
                            DaTai = false
                        });
                    }

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

        // user page thanh toán bằng MoMo (tạo giao dịch pending, chờ callback)
        public void CreatePendingMomo(int userId, string maGD, decimal amount)
        {
            var user = GetActiveUser(userId);
            if (user == null)
                throw new Exception("User inactive or not found");

            var cart = db.GioHangs
                .Include(x => x.ChiTietGioHangs)
                .FirstOrDefault(x => x.MaNguoiDung == userId);

            // 🔥 tạo giao dịch pending trước, chờ callback từ MoMo
            if (cart == null || !cart.ChiTietGioHangs.Any())
                throw new Exception("Cart empty");

            // tạo giao dịch với trạng thái Pending
            var giaoDich = new GiaoDich
            {
                MaGD = maGD,
                MaNguoiDung = userId,
                NgayMua = DateTime.UtcNow,
                TrangThai = "Pending",
                PhuongThuc = "MoMo",
                ThanhTien = amount
            };

            db.GiaoDiches.Add(giaoDich);

            // lưu chi tiết giao dịch để dùng khi callback hoàn tất
            foreach (var item in cart.ChiTietGioHangs)
            {
                db.ChiTietGiaoDiches.Add(new ChiTietGiaoDich
                {
                    MaGD = maGD,
                    MaGame = item.MaGame,
                    DonGia = item.DonGiaHienTai
                });
            }

            db.SaveChanges();
        }

        // callback từ MoMo khi giao dịch hoàn tất
        public async Task CompleteMomo(string maGD)
        {
            // callback từ MoMo khi giao dịch hoàn tất
            using var transaction = db.Database.BeginTransaction();

            // 🔥 bắt đầu transaction để đảm bảo tính toàn vẹn dữ liệu
            try
            {
                var gd = db.GiaoDiches
                    .Include(x => x.ChiTietGiaoDiches)
                    .FirstOrDefault(x => x.MaGD == maGD);

                if (gd == null)
                    throw new Exception("Transaction not found: " + maGD);

                var userId = gd.MaNguoiDung;

                var user = GetActiveUser(userId);
                if (user == null)
                    throw new Exception("User inactive or not found");

                // 🔥 trừ tiền sau khi xác nhận giao dịch thành công từ MoMo
                foreach (var item in gd.ChiTietGiaoDiches)
                {
                    var exists = db.ThuVienGames
                        .Any(x => x.MaNguoiDung == userId && x.MaGame == item.MaGame);

                    // 🔥 nếu chưa có trong thư viện thì mới add, tránh trùng lặp khi callback nhiều lần
                    if (!exists)
                    {
                        db.ThuVienGames.Add(new ThuVienGame
                        {
                            MaNguoiDung = userId,
                            MaGame = item.MaGame,
                            NgayMua = DateTime.UtcNow,
                            DaTai = false
                        });
                    }

                    // 🔥 cập nhật lượt tải và gửi realtime update đến client
                    var game = db.Games.Find(item.MaGame);
                    if (game != null)
                    {
                        game.SoLuotTai++;
                        await hub.Clients.All.SendAsync("UpdateDownload", game.MaGame, game.SoLuotTai);
                    }
                }

                // clear cart sau khi hoàn tất giao dịch
                var cart = db.GioHangs
                    .Include(x => x.ChiTietGioHangs)
                    .FirstOrDefault(x => x.MaNguoiDung == userId);

                if (cart != null)
                {
                    db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);
                }

                gd.TrangThai = "Success";

                db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "CompleteMomo error");
                throw;
            }
        }

        // callback từ MoMo khi giao dịch thất bại hoặc bị hủy
        public Task FailMomo(string maGD)
        {
            var gd = db.GiaoDiches.FirstOrDefault(x => x.MaGD == maGD);
            if (gd != null)
            {
                gd.TrangThai = "Failed";
                db.SaveChanges();
            }

            return Task.CompletedTask;
        }

        // user page nạp tiền vào tài khoản bằng MoMo (tạo giao dịch pending, chờ callback)
        public async Task CompleteTopup(int userId, decimal amount)
        {
            var user = GetActiveUser(userId);
            if (user == null) throw new Exception("User inactive or not found");

            user.SoDu += amount;
            await db.SaveChangesAsync();

            logger.LogInformation("Topup success: UserId={UserId}, Amount={Amount}, NewBalance={Balance}",
                userId, amount, user.SoDu);
        }

        // =========================
        // EVENT PAYMENT
        // =========================
        public string CreatePendingEventBalance(int userId, int eventId)
        {
            var user = GetActiveUser(userId);
            if (user == null) throw new Exception("User inactive or not found");

            // kiểm tra sự kiện tồn tại và có giá tiền
            var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
            if (ev == null) throw new Exception("Event not found");

            // tạo giao dịch pending để chờ thanh toán bằng balance
            var maGD = Guid.NewGuid().ToString();

            // 🔥 tạo giao dịch pending trước, chờ hoàn tất sau khi trừ tiền và tham gia sự kiện
            var giaoDich = new GiaoDich
            {
                MaGD = maGD,
                MaNguoiDung = userId,
                EventId = eventId,
                NgayMua = DateTime.UtcNow,
                TrangThai = "Pending",
                PhuongThuc = "Balance",
                LoaiGiaoDich = "EventJoin",
                ThanhTien = ev.Price,
                CreatedAt = DateTime.UtcNow
            };

            db.GiaoDiches.Add(giaoDich);
            db.SaveChanges();

            return maGD;
        }

        // hoàn tất giao dịch tham gia sự kiện bằng balance, trừ tiền và thêm vào danh sách tham gia
        public async Task<bool> CompleteEventBalance(string maGD)
        {
            // bắt đầu transaction để đảm bảo tính toàn vẹn dữ liệu khi trừ tiền và thêm vào sự kiện
            using var transaction = db.Database.BeginTransaction();

            try
            {
                // 🔥 lấy giao dịch và kiểm tra hợp lệ
                var gd = db.GiaoDiches
                    .Include(x => x.Event)
                    .FirstOrDefault(x => x.MaGD == maGD);

                // 🔥 chỉ xử lý giao dịch loại EventJoin và trạng thái Pending
                if (gd == null) return false;
                // 🔥 nếu không phải giao dịch tham gia sự kiện thì không xử lý
                if (gd.LoaiGiaoDich != "EventJoin") return false;
                // 🔥 nếu đã hoàn tất rồi thì không cần làm gì nữa
                if (gd.TrangThai == "Success") return true;
                // 🔥 nếu không phải trạng thái Pending thì không xử lý
                if (!gd.EventId.HasValue) return false;

                var user = GetActiveUser(gd.MaNguoiDung);
                if (user == null) return false;

                // 🔥 reload lại user để đảm bảo có số dư mới nhất trước khi trừ tiền
                db.Entry(user).Reload();

                // 🔥 kiểm tra đủ tiền trước khi trừ
                if (user.SoDu < gd.ThanhTien)
                    return false;

                // 🔥 kiểm tra nếu đã tham gia sự kiện rồi thì chỉ cần cập nhật trạng thái giao dịch mà không trừ tiền nữa
                if (eventParticipantService.IsJoined(gd.EventId.Value, gd.MaNguoiDung))
                {
                    gd.TrangThai = "Success";
                    db.SaveChanges();
                    transaction.Commit();
                    return true;
                }

                user.SoDu -= gd.ThanhTien;

                // 🔥 gọi service tham gia sự kiện có trả phí, nếu tham gia thất bại thì rollback và trả về false
                var joined = eventParticipantService.JoinPaid(gd.EventId.Value, gd.MaNguoiDung, gd.ThanhTien);
                if (!joined)
                {
                    transaction.Rollback();
                    return false;
                }

                gd.TrangThai = "Success";
                db.SaveChanges();
                transaction.Commit();

                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "CompleteEventBalance error");
                return false;
            }
        }

    }
}
