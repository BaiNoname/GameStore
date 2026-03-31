using GameStore.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VNPAY;
using VNPAY.Models;
using VNPAY.Models.Enums;
using VNPAY.Models.Exceptions;


namespace GameStore.Services
{
    public class VnpayServiceImpl : VnpayService
    {
        private readonly IVnpayClient _vnpayClient;
        private readonly GameStoreContext _db;
        private readonly ILogger<VnpayServiceImpl> _logger;
        private readonly IHubContext<GameStore.Hubs.GameHub> _hub;

        private const string ORDER_PREFIX = "ORDER_";
        private const string TOPUP_PREFIX = "TOPUP_";

        public VnpayServiceImpl(
            IVnpayClient vnpayClient,
            GameStoreContext db,
            ILogger<VnpayServiceImpl> logger,
            IHubContext<GameStore.Hubs.GameHub> hub)
        {
            _vnpayClient = vnpayClient;
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        // ═══════════════════════════════════════════════════════════════
        // TẠO URL THANH TOÁN ĐƠN HÀNG
        // ═══════════════════════════════════════════════════════════════
        public string CreatePaymentUrlForOrder(int userId, decimal amount, string baseUrl)
        {
            var maGD = Guid.NewGuid().ToString();

            var giaoDich = new GiaoDich
            {
                MaGD = maGD,
                MaNguoiDung = userId,
                NgayMua = DateTime.UtcNow,
                ThanhTien = amount,
                TrangThai = "Pending",
                PhuongThuc = "VNPay",
                CreatedAt = DateTime.UtcNow
            };

            // Snapshot giỏ hàng → ChiTietGiaoDich
            var cart = _db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .FirstOrDefault(g => g.MaNguoiDung == userId);

            if (cart != null)
            {
                foreach (var item in cart.ChiTietGioHangs)
                {
                    giaoDich.ChiTietGiaoDiches.Add(new ChiTietGiaoDich
                    {
                        MaGD = maGD,
                        MaGame = item.MaGame,
                        DonGia = item.DonGiaHienTai
                    });
                }
            }

            _db.GiaoDiches.Add(giaoDich);
            _db.SaveChanges();

            var paymentUrlInfo = _vnpayClient.CreatePaymentUrl(new VnpayPaymentRequest
            {
                Money = (double)amount,
                Description = $"{ORDER_PREFIX}{maGD}",
                BankCode = BankCode.ANY,
                Language = DisplayLanguage.Vietnamese
            });

            // DEBUG — xoá sau khi xong
            Console.WriteLine($"[DEBUG] Payment URL: {paymentUrlInfo.Url}");


            return paymentUrlInfo.Url;
        }

        // ═══════════════════════════════════════════════════════════════
        // TẠO URL NẠP TIỀN (TOP-UP)
        // ═══════════════════════════════════════════════════════════════
        public string CreatePaymentUrlForTopup(int userId, decimal amount, string baseUrl)
        {
            var maGD = Guid.NewGuid().ToString();

            var giaoDich = new GiaoDich
            {
                MaGD = maGD,
                MaNguoiDung = userId,
                NgayMua = DateTime.UtcNow,
                ThanhTien = amount,
                TrangThai = "Pending",
                PhuongThuc = "VNPay-Topup",
                CreatedAt = DateTime.UtcNow
            };

            _db.GiaoDiches.Add(giaoDich);
            _db.SaveChanges();

            var paymentUrlInfo = _vnpayClient.CreatePaymentUrl(new VnpayPaymentRequest
            {
                Money = (double)amount,
                Description = $"{TOPUP_PREFIX}{maGD}",
                BankCode = BankCode.ANY,
                Language = DisplayLanguage.Vietnamese
            });

            // DEBUG — xoá sau khi xong
            Console.WriteLine($"[DEBUG] Payment URL: {paymentUrlInfo.Url}");


            return paymentUrlInfo.Url;
        }
        // ═══════════════════════════════════════════════════════════════
        // Đổi tham số từ IQueryCollection → HttpRequest
        // (theo đúng cách thư viện phanxuanquang thiết kế)
        // ═══════════════════════════════════════════════════════════════
        public async Task<(bool isSuccess, string maGD, string loaiGD, string message)> HandleCallbackAsync(HttpRequest request)
        {
            bool paymentOk;
            string description;
            long vnpayTransactionId = 0;

            try
            {
                // GetPaymentResult tự verify chữ ký bên trong.
                // Nếu chữ ký sai hoặc thanh toán thất bại → ném VnpayException.
                // Nếu không ném → thanh toán hợp lệ và thành công.
                var result = _vnpayClient.GetPaymentResult(request);

                paymentOk = true;
                description = result.Description ?? request.Query["vnp_OrderInfo"].ToString() ?? "";
                vnpayTransactionId = result.VnpayTransactionId;

                _logger.LogInformation("VNPay success: PaymentId={id}, TransactionId={tid}",
                    result.PaymentId, result.VnpayTransactionId);
            }
            catch (VnpayException ex)
            {
                // Chữ ký sai, thanh toán thất bại hoặc bị hủy
                _logger.LogWarning("VNPay failed: ResponseCode={rc}, TransactionStatus={ts}, Message={msg}",
                    ex.PaymentResponseCode, ex.TransactionStatusCode, ex.Message);

                paymentOk = false;
                description = request.Query["vnp_OrderInfo"].ToString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay HandleCallbackAsync exception. Query={query}",
                    request.QueryString.Value);
                return (false, "", "", $"Lỗi hệ thống: {ex.Message}");
            }

            // ── Parse description để lấy maGD + loại GD ───────────────────────
            string maGD;
            string loaiGD;

            if (description.StartsWith(ORDER_PREFIX))
            {
                maGD = description[ORDER_PREFIX.Length..];
                loaiGD = "Order";
            }
            else if (description.StartsWith(TOPUP_PREFIX))
            {
                maGD = description[TOPUP_PREFIX.Length..];
                loaiGD = "Topup";
            }
            else
            {
                _logger.LogWarning("VNPay callback: unknown description: {desc}", description);
                return (false, "", "", "Không xác định được loại giao dịch");
            }

            // ── Tìm GiaoDich ──────────────────────────────────────────────────
            var giaoDich = await _db.GiaoDiches
                .FirstOrDefaultAsync(g => g.MaGD == maGD);

            if (giaoDich == null)
            {
                _logger.LogWarning("VNPay callback: GiaoDich not found: {maGD}", maGD);
                return (false, maGD, loaiGD, "Không tìm thấy giao dịch");
            }

            // ── Idempotent check ───────────────────────────────────────────────
            if (giaoDich.TrangThai != "Pending")
            {
                _logger.LogInformation("Already processed {maGD} → {status}", maGD, giaoDich.TrangThai);
                return (giaoDich.TrangThai == "Success", maGD, loaiGD, "Giao dịch đã xử lý trước đó");
            }

            // ── Lưu mã GD của VNPay ───────────────────────────────────────────
            giaoDich.VnpTransactionNo = vnpayTransactionId.ToString();

            if (!paymentOk)
            {
                giaoDich.TrangThai = "Failed";
                await _db.SaveChangesAsync();
                return (false, maGD, loaiGD, "Thanh toán không thành công từ VNPay");
            }

            // ── Xử lý nghiệp vụ ───────────────────────────────────────────────
            if (loaiGD == "Order")
                await ProcessOrderSuccessAsync(giaoDich);
            else
                await ProcessTopupSuccessAsync(giaoDich);

            return (true, maGD, loaiGD, "Thanh toán thành công");
        }

        // ═══════════════════════════════════════════════════════════════
        // Xử lý mua game thành công
        // ═══════════════════════════════════════════════════════════════
        private async Task ProcessOrderSuccessAsync(GiaoDich giaoDich)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var userId = giaoDich.MaNguoiDung;

                var chiTiets = await _db.ChiTietGiaoDiches
                    .Where(ct => ct.MaGD == giaoDich.MaGD)
                    .ToListAsync();

                foreach (var ct in chiTiets)
                {
                    var exists = await _db.ThuVienGames
                        .AnyAsync(x => x.MaNguoiDung == userId && x.MaGame == ct.MaGame);

                    if (!exists)
                    {
                        _db.ThuVienGames.Add(new ThuVienGame
                        {
                            MaNguoiDung = userId,
                            MaGame = ct.MaGame,
                            NgayMua = DateTime.UtcNow,
                            DaTai = false
                        });
                    }

                    var game = await _db.Games.FindAsync(ct.MaGame);
                    if (game != null)
                    {
                        game.SoLuotTai += 1;
                        await _hub.Clients.All.SendAsync("UpdateDownload", game.MaGame, game.SoLuotTai);
                    }
                }

                // Xóa giỏ hàng
                var cart = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                    .FirstOrDefaultAsync(g => g.MaNguoiDung == userId);

                if (cart != null)
                    _db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);

                giaoDich.TrangThai = "Success";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Order success: maGD={maGD}", giaoDich.MaGD);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Xử lý nạp tiền thành công
        // ═══════════════════════════════════════════════════════════════
        private async Task ProcessTopupSuccessAsync(GiaoDich giaoDich)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var user = await _db.NguoiDungs.FindAsync(giaoDich.MaNguoiDung);
                if (user == null) throw new Exception("User not found");

                user.SoDu += giaoDich.ThanhTien;
                giaoDich.TrangThai = "Success";

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Topup success: maGD={maGD}, amount={amount}", giaoDich.MaGD, giaoDich.ThanhTien);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}