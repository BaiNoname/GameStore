using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Controllers.Client.Library
{
    // Thư viện game của người dùng
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LibraryController : Controller
    {
        private readonly GameStoreContext db;
        private readonly RefundService refundService;

        public LibraryController(GameStoreContext _db, RefundService _refundService)
        {
            db = _db;
            refundService = _refundService;
        }

        // Lấy thông tin người dùng hiện tại từ cookie và kiểm tra tính hợp lệ
        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            // Kiểm tra nếu người dùng chưa đăng nhập
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            // Lấy claim chứa UserId từ cookie
            var claim = User.FindFirst("UserId")?.Value;

            // Nếu claim không tồn tại hoặc không phải là số nguyên hợp lệ, trả về null 
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            // Truy vấn cơ sở dữ liệu để lấy thông tin người dùng dựa trên UserId và kiểm tra xem tài khoản có đang hoạt động hay không
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

            // Nếu không tìm thấy người dùng hoặc tài khoản không hoạt động, xóa cookie và trả về null
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

        // Hiển thị danh sách game trong thư viện của người dùng
        public async Task<IActionResult> Index()
        {
            ViewBag.HideSubBar = true;

            // Lấy thông tin người dùng hiện tại và kiểm tra nếu chưa đăng nhập thì chuyển hướng đến trang đăng nhập
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Truy vấn cơ sở dữ liệu để lấy danh sách game trong thư viện của người dùng, bao gồm thông tin chi tiết về game
            var data = db.ThuVienGames
                .Include(x => x.Game)
                .Where(x => x.MaNguoiDung == user.MaNguoiDung)
                .ToList();

            // ===== Dữ liệu cho chức năng hoàn trả (refund) =====
            // Tập game đang có yêu cầu hoàn trả (Pending/Approved) -> để ẩn nút Refund
            var activeRefundIds = refundService.GetActiveRefundGameIds(user.MaNguoiDung);

            // Tập game còn trong hạn hoàn trả (được phép bấm Refund)
            var refundableIds = data
                .Where(x => refundService.CanRefund(x, activeRefundIds))
                .Select(x => x.MaGame)
                .ToHashSet();

            ViewBag.RefundableIds = refundableIds;
            ViewBag.ActiveRefundIds = activeRefundIds;
            ViewBag.RefundReasons = refundService.Reasons;
            ViewBag.RefundWindowMinutes = refundService.RefundWindowMinutes;
            ViewBag.MyRefunds = refundService.GetUserRefunds(user.MaNguoiDung);

            return View(data);
        }

        // Người dùng gửi yêu cầu hoàn trả game
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(string gameId, string lyDo, string noiDung)
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            var (ok, message) = refundService.RequestRefund(user.MaNguoiDung, gameId, lyDo, noiDung);

            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";

            return RedirectToAction("Index");
        }

        // Xử lý yêu cầu tải game từ thư viện của người dùng
        public async Task<IActionResult> Download(string id)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra nếu chưa đăng nhập thì chuyển hướng đến trang đăng nhập
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Truy vấn cơ sở dữ liệu để tìm kiếm game trong thư viện của người dùng dựa trên MaGame và MaNguoiDung
            var item = db.ThuVienGames
                .Include(x => x.Game)
                .FirstOrDefault(x => x.MaNguoiDung == user.MaNguoiDung && x.MaGame == id);

            // Nếu không tìm thấy game trong thư viện của người dùng, trả về lỗi 404 Not Found
            if (item == null)
                return NotFound();

            // Nếu game chưa được đánh dấu là đã tải, cập nhật trạng thái và lưu thay đổi vào cơ sở dữ liệu
            if (!item.DaTai)
            {
                item.DaTai = true;
                db.SaveChanges();
            }

            // Kiểm tra nếu thông tin game không tồn tại hoặc đường dẫn tải game không hợp lệ, trả về lỗi 404 Not Found
            if (item.Game == null || string.IsNullOrWhiteSpace(item.Game.LinkGame))
                return NotFound();

            return Redirect(item.Game.LinkGame);
        }
    }
}