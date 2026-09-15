using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class NotificationServiceImpl : NotificationService
    {
        private readonly GameStoreContext db;

        public NotificationServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public List<TickerItem> GetTickerItems()
        {
            var now = DateTime.UtcNow;
            var items = new List<TickerItem>();

            // 1) Thông báo admin tự tạo (Custom) đang bật, chưa hết hạn
            var customs = db.ThongBaos
                .Where(t => t.IsActive && t.LoaiTB == "Custom"
                            && (t.NgayHetHan == null || t.NgayHetHan >= now))
                .OrderBy(t => t.ThuTu).ThenByDescending(t => t.NgayTao)
                .Take(10)
                .ToList();
            foreach (var t in customs)
                items.Add(new TickerItem { Icon = "📢", Text = t.NoiDung, Link = t.Link });

            // 2) Lượt mua gần đây: "Người dùng X vừa mua game Y"
            var purchases = (from ct in db.ChiTietGiaoDiches
                             join g in db.GiaoDiches on ct.MaGD equals g.MaGD
                             join u in db.NguoiDungs on g.MaNguoiDung equals u.MaNguoiDung
                             join game in db.Games on ct.MaGame equals game.MaGame
                             where g.TrangThai == "Success" && g.LoaiGiaoDich == "GamePurchase"
                             orderby g.NgayMua descending
                             select new { u.TenNguoiDung, game.TenGame, game.MaGame })
                            .Take(8).ToList();
            foreach (var p in purchases)
                items.Add(new TickerItem
                {
                    Icon = "🛒",
                    Text = $"{p.TenNguoiDung} vừa mua {p.TenGame}",
                    Link = $"/Game/Detail/{Uri.EscapeDataString(p.MaGame)}"
                });

            // 3) Game mới (mới ra mắt gần đây)
            var newGames = db.Games
                .OrderByDescending(x => x.NgayRaMat)
                .Take(5)
                .Select(x => new { x.TenGame, x.MaGame })
                .ToList();
            foreach (var gm in newGames)
                items.Add(new TickerItem
                {
                    Icon = "🎮",
                    Text = $"Game mới: {gm.TenGame}",
                    Link = $"/Game/Detail/{Uri.EscapeDataString(gm.MaGame)}"
                });

            // 4) Sự kiện sắp diễn ra
            var events = db.Events
                .Where(e => e.StartAt >= now)
                .OrderBy(e => e.StartAt)
                .Take(5)
                .Select(e => new { e.Title, e.StartAt })
                .ToList();
            foreach (var ev in events)
                items.Add(new TickerItem
                {
                    Icon = "📅",
                    Text = $"Sự kiện sắp diễn ra: {ev.Title} ({ev.StartAt.ToLocalTime():dd/MM HH:mm})",
                    Link = "/Event"
                });

            return items;
        }

        // ---------------- Admin ----------------
        public List<ThongBao> FindAll(string keyword, int page, int pageSize, out int totalPages)
        {
            var q = db.ThongBaos.Where(t => t.LoaiTB == "Custom").AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(t => t.NoiDung.Contains(keyword));

            int total = q.Count();
            totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            return q.OrderBy(t => t.ThuTu).ThenByDescending(t => t.NgayTao)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public ThongBao? GetById(int maTB) => db.ThongBaos.FirstOrDefault(t => t.MaTB == maTB);

        public (bool ok, string message) Create(ThongBao tb)
        {
            if (string.IsNullOrWhiteSpace(tb.NoiDung))
                return (false, "Vui lòng nhập nội dung thông báo.");

            tb.LoaiTB = "Custom";
            tb.NgayTao = DateTime.UtcNow;
            db.ThongBaos.Add(tb);
            db.SaveChanges();
            return (true, "Đã tạo thông báo.");
        }

        public (bool ok, string message) Update(ThongBao tb)
        {
            var e = db.ThongBaos.FirstOrDefault(x => x.MaTB == tb.MaTB);
            if (e == null) return (false, "Không tìm thấy thông báo.");
            if (string.IsNullOrWhiteSpace(tb.NoiDung))
                return (false, "Vui lòng nhập nội dung thông báo.");

            e.NoiDung = tb.NoiDung;
            e.Link = tb.Link;
            e.ThuTu = tb.ThuTu;
            e.IsActive = tb.IsActive;
            e.NgayHetHan = tb.NgayHetHan;
            db.SaveChanges();
            return (true, "Đã cập nhật thông báo.");
        }

        public (bool ok, string message) ToggleActive(int maTB)
        {
            var e = db.ThongBaos.FirstOrDefault(x => x.MaTB == maTB);
            if (e == null) return (false, "Không tìm thấy thông báo.");
            e.IsActive = !e.IsActive;
            db.SaveChanges();
            return (true, e.IsActive ? "Đã bật thông báo." : "Đã tắt thông báo.");
        }

        public (bool ok, string message) Delete(int maTB)
        {
            var e = db.ThongBaos.FirstOrDefault(x => x.MaTB == maTB);
            if (e == null) return (false, "Không tìm thấy thông báo.");
            db.ThongBaos.Remove(e);
            db.SaveChanges();
            return (true, "Đã xóa thông báo.");
        }
    }
}
