using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GameStore.Services
{
    public class VnpayServiceImpl : VnpayService
    {
        private readonly GameStoreContext _db;
        private readonly ILogger<VnpayServiceImpl> _logger;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _callbackUrl;
        private readonly string _version;
        private readonly string _orderType;

        public VnpayServiceImpl(GameStoreContext db, IConfiguration configuration, ILogger<VnpayServiceImpl> logger)
        {
            _db = db;
            _logger = logger;

            var section = configuration.GetSection("VNPAY");

            _tmnCode = section["TmnCode"]!;
            _hashSecret = section["HashSecret"]!;
            _baseUrl = section["BaseUrl"]!;
            _callbackUrl = section["CallbackUrl"]!;
            _version = section["Version"] ?? "2.1.0";
            _orderType = section["OrderType"] ?? "other";
        }

        public string CreatePaymentUrlForOrder(int userId, decimal amount, string returnUrl)
            => CreateVnpayUrl(userId, amount, "VNPAY", returnUrl);

        public string CreateTopupUrl(int userId, decimal amount, string returnUrl)
            => CreateVnpayUrl(userId, amount, "Topup", returnUrl);

        private string CreateVnpayUrl(int userId, decimal amount, string phuongThuc, string returnUrl)
        {
            var maGD = Guid.NewGuid().ToString();

            // 🔥 lấy cart trước (để snapshot)
            var cart = _db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .FirstOrDefault(g => g.MaNguoiDung == userId);

            var gd = new GiaoDich
            {
                MaGD = maGD,
                MaNguoiDung = userId,
                NgayMua = DateTime.Now,
                TrangThai = "Pending",
                PhuongThuc = phuongThuc,
                ThanhTien = amount
            };

            _db.GiaoDiches.Add(gd);

            // 🔥 snapshot cart → lưu vào ChiTietGiaoDich
            if (phuongThuc == "VNPAY" && cart != null)
            {
                foreach (var item in cart.ChiTietGioHangs)
                {
                    _db.ChiTietGiaoDiches.Add(new ChiTietGiaoDich
                    {
                        MaGD = maGD,
                        MaGame = item.MaGame,
                        DonGia = item.DonGiaHienTai
                    });
                }
            }

            _db.SaveChanges();

            var vnpParams = new SortedDictionary<string, string?>
            {
                { "vnp_Version", _version },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(amount * 100)).ToString() },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", maGD },
                { "vnp_OrderInfo", $"Payment {maGD}" },
                { "vnp_OrderType", _orderType },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", returnUrl ?? _callbackUrl },
                { "vnp_IpAddr", "127.0.0.1" },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
            };

            var queryBuilder = new StringBuilder();
            var rawHashBuilder = new StringBuilder();

            foreach (var kv in vnpParams)
            {
                if (kv.Value == null) continue;

                // ✅ query: encode
                if (queryBuilder.Length > 0) queryBuilder.Append('&');
                queryBuilder.Append(kv.Key)
                    .Append('=')
                    .Append(Uri.EscapeDataString(kv.Value));

                // ❌ HASH: KHÔNG encode
                if (rawHashBuilder.Length > 0) rawHashBuilder.Append('&');
                rawHashBuilder.Append(kv.Key)
                    .Append('=')
                    .Append(kv.Value);
            }

            var secureHash = HmacSha512(_hashSecret, rawHashBuilder.ToString());

            queryBuilder.Append("&vnp_SecureHash=").Append(secureHash);
            queryBuilder.Append("&vnp_SecureHashType=SHA512");

            return $"{_baseUrl}?{queryBuilder}";
        }

        public bool HandleVnpayResult(HttpRequest request, out string message)
        {
            message = "";

            var query = request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            query.TryGetValue("vnp_SecureHash", out var secureHash);

            var sorted = new SortedDictionary<string, string>();
            foreach (var kv in query)
            {
                if (kv.Key == "vnp_SecureHash" || kv.Key == "vnp_SecureHashType") continue;
                sorted[kv.Key] = kv.Value;
            }

            var raw = string.Join("&", sorted.Select(kv =>
                $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            var computed = HmacSha512(_hashSecret, raw);

            if (secureHash != computed)
            {
                message = "Invalid signature";
                return false;
            }

            var txnRef = sorted["vnp_TxnRef"];
            var responseCode = sorted["vnp_ResponseCode"];
            var amount = long.Parse(sorted["vnp_Amount"]) / 100;

            var gd = _db.GiaoDiches
                .Include(x => x.ChiTietGiaoDiches)
                .FirstOrDefault(x => x.MaGD == txnRef);

            if (gd == null)
            {
                message = "Not found";
                return false;
            }

            // chống double call
            if (gd.TrangThai == "Success")
            {
                message = "Already processed";
                return true;
            }

            if (gd.ThanhTien != amount)
            {
                message = "Amount mismatch";
                return false;
            }

            if (responseCode != "00")
            {
                gd.TrangThai = "Failed";
                _db.SaveChanges();
                message = "Failed";
                return false;
            }

            // ===== SUCCESS =====
            gd.TrangThai = "Success";

            if (gd.PhuongThuc == "Topup")
            {
                var user = _db.NguoiDungs.Find(gd.MaNguoiDung);
                if (user != null)
                    user.SoDu += gd.ThanhTien;
            }
            else
            {
                foreach (var item in gd.ChiTietGiaoDiches)
                {
                    var game = _db.Games.Find(item.MaGame);
                    if (game != null)
                    {
                        game.SoLuotTai += 1;
                    }

                    // 🔥 FIX duplicate library
                    var exists = _db.ThuVienGames
                        .Any(x => x.MaNguoiDung == gd.MaNguoiDung && x.MaGame == item.MaGame);

                    if (!exists)
                    {
                        _db.ThuVienGames.Add(new ThuVienGame
                        {
                            MaNguoiDung = gd.MaNguoiDung,
                            MaGame = item.MaGame,
                            NgayMua = DateTime.Now
                        });
                    }
                }

                // clear cart
                var cart = _db.GioHangs
                    .Include(x => x.ChiTietGioHangs)
                    .FirstOrDefault(x => x.MaNguoiDung == gd.MaNguoiDung);

                if (cart != null)
                    _db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);
            }

            _db.SaveChanges();

            message = "OK";
            return true;
        }

        private static string HmacSha512(string key, string input)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}