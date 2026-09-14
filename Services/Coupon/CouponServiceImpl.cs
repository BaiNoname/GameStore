using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class CouponServiceImpl : CouponService
    {
        private readonly GameStoreContext db;

        public CouponServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Mã còn hiệu lực về mặt thời gian + đang bật
        private static bool IsLive(KhuyenMai k, DateTime now)
        {
            if (!k.IsActive) return false;
            if (k.NgayBatDau.HasValue && now < k.NgayBatDau.Value) return false;
            if (k.NgayKetThuc.HasValue && now > k.NgayKetThuc.Value) return false;
            return true;
        }

        public List<KhuyenMai> GetPublicList()
        {
            var now = DateTime.UtcNow;
            return db.KhuyenMais
                .Where(k => k.IsActive
                            && (k.NgayBatDau == null || k.NgayBatDau <= now)
                            && (k.NgayKetThuc == null || k.NgayKetThuc >= now))
                .OrderByDescending(k => k.CreatedAt)
                .ToList();
        }

        public HashSet<int> GetClaimedIds(int userId)
        {
            return db.NguoiDungKhuyenMais
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.MaKM)
                .ToHashSet();
        }

        public (bool ok, string message) Claim(int userId, int maKM)
        {
            using var tx = db.Database.BeginTransaction();
            try
            {
                var km = db.KhuyenMais.FirstOrDefault(k => k.MaKM == maKM);
                if (km == null) return (false, "Không tìm thấy mã khuyến mãi.");
                if (!IsLive(km, DateTime.UtcNow)) return (false, "Mã đã hết hạn hoặc chưa mở.");

                bool claimed = db.NguoiDungKhuyenMais.Any(x => x.MaNguoiDung == userId && x.MaKM == maKM);
                if (claimed) return (false, "Bạn đã lấy mã này rồi.");

                if (km.DaDung >= km.SoLuong) return (false, "Mã đã hết lượt.");

                // Trừ số lượng NGAY khi lấy
                km.DaDung += 1;
                db.NguoiDungKhuyenMais.Add(new NguoiDungKhuyenMai
                {
                    MaNguoiDung = userId,
                    MaKM = maKM,
                    NgayLay = DateTime.UtcNow,
                    DaSuDung = false
                });

                db.SaveChanges();
                tx.Commit();
                return (true, $"Đã lấy mã {km.Code} thành công!");
            }
            catch
            {
                tx.Rollback();
                return (false, "Có lỗi khi lấy mã, vui lòng thử lại.");
            }
        }

        public List<KhuyenMai> GetMyUsableCoupons(int userId)
        {
            var now = DateTime.UtcNow;
            return db.NguoiDungKhuyenMais
                .Where(x => x.MaNguoiDung == userId && !x.DaSuDung)
                .Include(x => x.KhuyenMai)
                .Select(x => x.KhuyenMai!)
                .Where(k => k.IsActive
                            && (k.NgayBatDau == null || k.NgayBatDau <= now)
                            && (k.NgayKetThuc == null || k.NgayKetThuc >= now))
                .ToList();
        }

        public decimal ComputeDiscount(KhuyenMai km, decimal orderTotal)
        {
            decimal discount;
            if (km.LoaiGiam == "Percent")
            {
                discount = orderTotal * km.GiaTri / 100m;
                if (km.GiamToiDa.HasValue && discount > km.GiamToiDa.Value)
                    discount = km.GiamToiDa.Value;
            }
            else // Fixed
            {
                discount = km.GiaTri;
            }

            if (discount > orderTotal) discount = orderTotal;   // không giảm quá tổng đơn
            if (discount < 0) discount = 0;
            return Math.Round(discount, 0);
        }

        public KhuyenMai? GetUsableById(int userId, int maKM)
        {
            var link = db.NguoiDungKhuyenMais
                .Include(x => x.KhuyenMai)
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaKM == maKM && !x.DaSuDung);

            if (link?.KhuyenMai == null) return null;
            if (!IsLive(link.KhuyenMai, DateTime.UtcNow)) return null;
            return link.KhuyenMai;
        }

        public (bool ok, string message, decimal discount, KhuyenMai? km) TryApply(int userId, string code, decimal orderTotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Vui lòng nhập mã.", 0, null);

            code = code.Trim();
            var km = db.KhuyenMais.FirstOrDefault(k => k.Code == code);
            if (km == null) return (false, "Mã không tồn tại.", 0, null);
            if (!IsLive(km, DateTime.UtcNow)) return (false, "Mã đã hết hạn hoặc chưa mở.", 0, null);

            var link = db.NguoiDungKhuyenMais
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaKM == km.MaKM);
            if (link == null) return (false, "Bạn chưa lấy mã này. Vào trang Khuyến mãi để lấy mã.", 0, null);
            if (link.DaSuDung) return (false, "Bạn đã dùng mã này rồi.", 0, null);

            if (km.DonToiThieu.HasValue && orderTotal < km.DonToiThieu.Value)
                return (false, $"Đơn tối thiểu {km.DonToiThieu.Value:N0}đ để dùng mã này.", 0, null);

            var discount = ComputeDiscount(km, orderTotal);
            if (discount <= 0) return (false, "Mã không áp dụng được cho đơn này.", 0, null);

            return (true, $"Áp mã {km.Code} thành công! Giảm {discount:N0}đ.", discount, km);
        }

        public void MarkUsed(int userId, int maKM, string maGD)
        {
            var link = db.NguoiDungKhuyenMais
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaKM == maKM);
            if (link == null || link.DaSuDung) return;

            link.DaSuDung = true;
            link.NgaySuDung = DateTime.UtcNow;
            link.MaGD = maGD;
            db.SaveChanges();
        }

        // ---------------- Admin ----------------
        public List<KhuyenMai> FindAll(string keyword, int page, int pageSize, out int totalPages)
        {
            var q = db.KhuyenMais.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(k => k.Code.Contains(keyword) || (k.MoTa != null && k.MoTa.Contains(keyword)));

            int total = q.Count();
            totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            return q.OrderByDescending(k => k.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
        }

        public KhuyenMai? GetById(int maKM) => db.KhuyenMais.FirstOrDefault(k => k.MaKM == maKM);

        private static (bool ok, string message) Validate(KhuyenMai km)
        {
            if (string.IsNullOrWhiteSpace(km.Code)) return (false, "Vui lòng nhập mã code.");
            if (km.LoaiGiam != "Percent" && km.LoaiGiam != "Fixed") return (false, "Loại giảm không hợp lệ.");
            if (km.GiaTri <= 0) return (false, "Giá trị giảm phải lớn hơn 0.");
            if (km.LoaiGiam == "Percent" && km.GiaTri > 100) return (false, "Giảm theo % không vượt quá 100.");
            if (km.SoLuong <= 0) return (false, "Số lượng phải lớn hơn 0.");
            if (km.NgayBatDau.HasValue && km.NgayKetThuc.HasValue && km.NgayKetThuc < km.NgayBatDau)
                return (false, "Ngày kết thúc phải sau ngày bắt đầu.");
            return (true, "");
        }

        public (bool ok, string message) Create(KhuyenMai km)
        {
            var (ok, msg) = Validate(km);
            if (!ok) return (false, msg);

            km.Code = km.Code.Trim();
            if (db.KhuyenMais.Any(k => k.Code == km.Code))
                return (false, "Mã code đã tồn tại.");

            km.DaDung = 0;
            km.CreatedAt = DateTime.UtcNow;
            db.KhuyenMais.Add(km);
            db.SaveChanges();
            return (true, "Đã tạo mã khuyến mãi.");
        }

        public (bool ok, string message) Update(KhuyenMai km)
        {
            var existing = db.KhuyenMais.FirstOrDefault(k => k.MaKM == km.MaKM);
            if (existing == null) return (false, "Không tìm thấy mã.");

            var (ok, msg) = Validate(km);
            if (!ok) return (false, msg);

            km.Code = km.Code.Trim();
            if (db.KhuyenMais.Any(k => k.Code == km.Code && k.MaKM != km.MaKM))
                return (false, "Mã code đã tồn tại.");

            if (km.SoLuong < existing.DaDung)
                return (false, $"Số lượng không được nhỏ hơn số đã phát ({existing.DaDung}).");

            existing.Code = km.Code;
            existing.MoTa = km.MoTa;
            existing.LoaiGiam = km.LoaiGiam;
            existing.GiaTri = km.GiaTri;
            existing.GiamToiDa = km.GiamToiDa;
            existing.DonToiThieu = km.DonToiThieu;
            existing.SoLuong = km.SoLuong;
            existing.NgayBatDau = km.NgayBatDau;
            existing.NgayKetThuc = km.NgayKetThuc;
            existing.IsActive = km.IsActive;

            db.SaveChanges();
            return (true, "Đã cập nhật mã khuyến mãi.");
        }

        public (bool ok, string message) ToggleActive(int maKM)
        {
            var km = db.KhuyenMais.FirstOrDefault(k => k.MaKM == maKM);
            if (km == null) return (false, "Không tìm thấy mã.");
            km.IsActive = !km.IsActive;
            db.SaveChanges();
            return (true, km.IsActive ? "Đã bật mã." : "Đã tắt mã.");
        }

        public (bool ok, string message) Delete(int maKM)
        {
            var km = db.KhuyenMais.FirstOrDefault(k => k.MaKM == maKM);
            if (km == null) return (false, "Không tìm thấy mã.");

            // Nếu đã có người lấy thì không xoá cứng -> tắt để giữ lịch sử
            if (db.NguoiDungKhuyenMais.Any(x => x.MaKM == maKM))
            {
                km.IsActive = false;
                db.SaveChanges();
                return (true, "Mã đã có người lấy nên được TẮT thay vì xoá (giữ lịch sử).");
            }

            db.KhuyenMais.Remove(km);
            db.SaveChanges();
            return (true, "Đã xoá mã khuyến mãi.");
        }
    }
}
