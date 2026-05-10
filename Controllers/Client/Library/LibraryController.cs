using GameStore.Models;
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

        public LibraryController(GameStoreContext _db)
        {
            db = _db;
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

            return View(data);
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