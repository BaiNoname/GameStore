using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    // Controller quản lý tài khoản người dùng, bao gồm xem thông tin cá nhân, đổi tên và đổi mật khẩu
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AccountController : Controller
    {
        private readonly AuthService authService;
        private readonly GameStoreContext db;
        private readonly UserIconEffectService userIconEffectService;

        public AccountController(AuthService _authService, GameStoreContext _db, UserIconEffectService _userIconEffectService)
        {
            authService = _authService;
            db = _db;
            userIconEffectService = _userIconEffectService;
        }

        // Phương thức helper để lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            // Nếu người dùng chưa đăng nhập, trả về null
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            // Lấy claim chứa UserId từ token, nếu không có hoặc không hợp lệ thì trả về null
            var claim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            // Truy vấn cơ sở dữ liệu để lấy thông tin người dùng dựa trên UserId và kiểm tra xem tài khoản có còn hoạt động hay không
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

            // Nếu không tìm thấy người dùng hoặc tài khoản đã bị vô hiệu hóa, xóa session và đăng xuất
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

        // Hiển thị trang thông tin cá nhân của người dùng, nếu chưa đăng nhập hoặc tài khoản không còn hoạt động thì chuyển hướng đến trang đăng nhập
        public async Task<IActionResult> Profile()
        {
            ViewBag.HideSubBar = true;

            // Lấy thông tin người dùng hiện tại, nếu không có hoặc tài khoản không còn hoạt động thì chuyển hướng đến trang đăng nhập
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Lấy CSS class của hiệu ứng icon đang được trang bị để hiển thị trên giao diện
            ViewBag.EquippedEffectCssClass = userIconEffectService.GetEquippedCssClass(user.MaNguoiDung);

            return View(user);
        }

        // Phương thức xử lý yêu cầu đổi tên người dùng, nếu chưa đăng nhập hoặc tài khoản không còn hoạt động thì chuyển hướng đến trang đăng nhập
        [HttpPost]
        public async Task<IActionResult> UpdateName(string tenNguoiDung)
        {
            // Lấy thông tin người dùng hiện tại, nếu không có hoặc tài khoản không còn hoạt động thì chuyển hướng đến trang đăng nhập
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Gọi service để cập nhật tên người dùng, nếu có lỗi sẽ lưu thông báo lỗi vào TempData để hiển thị trên giao diện, ngược lại sẽ lưu thông báo thành công
            bool result = authService.UpdateName(user.MaNguoiDung, tenNguoiDung, out string msg);

            if (!result)
                TempData["Err"] = msg;
            else
                TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }

        // Phương thức xử lý yêu cầu đổi mật khẩu, nếu chưa đăng nhập hoặc tài khoản không còn hoạt động thì chuyển hướng đến trang đăng nhập
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            // Lấy thông tin người dùng hiện tại, nếu không có hoặc tài khoản không còn hoạt động thì chuyển hướng đến trang đăng nhập
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Gọi service để đổi mật khẩu, nếu có lỗi sẽ lưu thông báo lỗi vào TempData để hiển thị trên giao diện, ngược lại sẽ xóa session và đăng xuất người dùng, sau đó lưu thông báo thành công vào TempData
            bool success = authService.ChangePassword(user.MaNguoiDung, oldPass, newPass, confirmPass, out string message);

            if (!success)
            {
                TempData["Err"] = message;
                return RedirectToAction("Profile");
            }

            // Nếu đổi mật khẩu thành công, xóa session và đăng xuất người dùng để yêu cầu đăng nhập lại với mật khẩu mới
            HttpContext.Session.Clear();

            // Đăng xuất người dùng bằng cách xóa cookie xác thực
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại 🔐";
            TempData["ToastType"] = "success";

            return RedirectToAction("Login", "Auth");
        }
    }
}