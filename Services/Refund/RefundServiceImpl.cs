using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class RefundServiceImpl : RefundService
    {
        private readonly GameStoreContext db;

        public RefundServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public int RefundWindowMinutes => 30;

        public string[] Reasons => new[]
        {
            "Không đúng như mô tả",
            "Lỗi kỹ thuật / không chạy được",
            "Mua nhầm",
            "Không hài lòng về gameplay",
            "Yêu cầu cấu hình quá cao",
            "Lý do khác"
        };

        public HashSet<string> GetActiveRefundGameIds(int userId)
        {
            // Chỉ tính yêu cầu ĐANG chờ xử lý (Pending).
            // Approved/Rejected là đã xử lý xong -> không chặn lần mua lại sau này.
            return db.HoanTras
                .Where(h => h.MaNguoiDung == userId && h.TrangThai == "Pending")
                .Select(h => h.MaGame)
                .ToHashSet();
        }

        public bool CanRefund(ThuVienGame item, HashSet<string> activeRefundGameIds)
        {
            if (item == null) return false;
            if (activeRefundGameIds.Contains(item.MaGame)) return false;
            // Đã tải game về máy -> không cho hoàn trả (vì không có DRM để thu hồi file)
            if (item.DaTai) return false;
            return (DateTime.UtcNow - item.NgayMua).TotalMinutes <= RefundWindowMinutes;
        }

        public (bool ok, string message) RequestRefund(int userId, string gameId, string lyDo, string noiDung)
        {
            var lib = db.ThuVienGames.FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == gameId);
            if (lib == null)
                return (false, "Bạn không sở hữu game này.");

            if ((DateTime.UtcNow - lib.NgayMua).TotalMinutes > RefundWindowMinutes)
                return (false, $"Đã quá {RefundWindowMinutes} phút kể từ khi mua, không thể hoàn trả.");

            // Đã tải game về máy -> không cho hoàn trả
            if (lib.DaTai)
                return (false, "Game đã được tải về nên không thể hoàn trả.");

            // Chỉ chặn khi đang có 1 yêu cầu Pending cho chính game này.
            // (Đã Approved/Rejected trước đó không chặn, vì đây là lượt sở hữu mới.)
            bool dangCho = db.HoanTras.Any(h => h.MaNguoiDung == userId && h.MaGame == gameId
                                                && h.TrangThai == "Pending");
            if (dangCho)
                return (false, "Bạn đang có yêu cầu hoàn trả chờ duyệt cho game này.");

            if (string.IsNullOrWhiteSpace(lyDo))
                return (false, "Vui lòng chọn lý do hoàn trả.");

            // Số tiền hoàn: lấy từ chi tiết giao dịch mua gần nhất, nếu không có thì lấy giá game hiện tại
            decimal amount;
            string? maGD = null;

            var ct = (from c in db.ChiTietGiaoDiches
                      join g in db.GiaoDiches on c.MaGD equals g.MaGD
                      where g.MaNguoiDung == userId && c.MaGame == gameId && g.TrangThai == "Success"
                      orderby g.NgayMua descending
                      select new { c.DonGia, c.MaGD }).FirstOrDefault();

            if (ct != null)
            {
                amount = ct.DonGia;
                maGD = ct.MaGD;
            }
            else
            {
                amount = db.Games.Where(x => x.MaGame == gameId).Select(x => x.Gia).FirstOrDefault();
            }

            var ht = new HoanTra
            {
                MaNguoiDung = userId,
                MaGame = gameId,
                MaGD = maGD,
                LyDo = lyDo,
                NoiDung = noiDung,
                SoTienHoan = amount,
                TrangThai = "Pending",
                NgayYeuCau = DateTime.UtcNow
            };
            db.HoanTras.Add(ht);
            db.SaveChanges();

            return (true, "Đã gửi yêu cầu hoàn trả. Vui lòng chờ admin duyệt.");
        }

        public List<HoanTra> GetUserRefunds(int userId)
        {
            return db.HoanTras
                .Include(h => h.Game)
                .Where(h => h.MaNguoiDung == userId)
                .OrderByDescending(h => h.NgayYeuCau)
                .ToList();
        }

        public List<HoanTra> FindAll(string status, int page, int pageSize, out int totalPages)
        {
            var q = db.HoanTras
                .Include(h => h.NguoiDung)
                .Include(h => h.Game)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(h => h.TrangThai == status);

            int total = q.Count();
            totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            // Ưu tiên Pending lên đầu, rồi mới nhất trước
            return q
                .OrderBy(h => h.TrangThai == "Pending" ? 0 : 1)
                .ThenByDescending(h => h.NgayYeuCau)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public (bool ok, string message) Approve(int maHoanTra)
        {
            var ht = db.HoanTras.FirstOrDefault(h => h.MaHoanTra == maHoanTra);
            if (ht == null) return (false, "Không tìm thấy yêu cầu hoàn trả.");
            if (ht.TrangThai != "Pending") return (false, "Yêu cầu này đã được xử lý.");

            using var tx = db.Database.BeginTransaction();
            try
            {
                var user = db.NguoiDungs.FirstOrDefault(u => u.MaNguoiDung == ht.MaNguoiDung);
                if (user == null) return (false, "Không tìm thấy người dùng.");

                // 1) Hoàn tiền về số dư
                user.SoDu += ht.SoTienHoan;

                // 2) Trừ game khỏi thư viện
                var lib = db.ThuVienGames.FirstOrDefault(x => x.MaNguoiDung == ht.MaNguoiDung && x.MaGame == ht.MaGame);
                if (lib != null) db.ThuVienGames.Remove(lib);

                // 3) Ghi 1 giao dịch hoàn tiền để hiện trong lịch sử/khu admin
                var gd = new GiaoDich
                {
                    MaGD = Guid.NewGuid().ToString(),
                    MaNguoiDung = ht.MaNguoiDung,
                    NgayMua = DateTime.UtcNow,
                    ThanhTien = ht.SoTienHoan,
                    TrangThai = "Success",
                    PhuongThuc = "Refund",
                    LoaiGiaoDich = "Refund",
                    CreatedAt = DateTime.UtcNow
                };
                db.GiaoDiches.Add(gd);

                // 4) Cập nhật trạng thái yêu cầu
                ht.TrangThai = "Approved";
                ht.NgayXuLy = DateTime.UtcNow;

                db.SaveChanges();
                tx.Commit();
                return (true, "Đã duyệt hoàn trả và hoàn tiền cho người dùng.");
            }
            catch
            {
                tx.Rollback();
                return (false, "Có lỗi khi xử lý hoàn trả, vui lòng thử lại.");
            }
        }

        public (bool ok, string message) Reject(int maHoanTra, string ghiChu)
        {
            var ht = db.HoanTras.FirstOrDefault(h => h.MaHoanTra == maHoanTra);
            if (ht == null) return (false, "Không tìm thấy yêu cầu hoàn trả.");
            if (ht.TrangThai != "Pending") return (false, "Yêu cầu này đã được xử lý.");

            ht.TrangThai = "Rejected";
            ht.NgayXuLy = DateTime.UtcNow;
            ht.GhiChuAdmin = ghiChu;
            db.SaveChanges();
            return (true, "Đã từ chối yêu cầu hoàn trả.");
        }
    }
}