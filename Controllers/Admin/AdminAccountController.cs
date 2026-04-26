using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin/account")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminAccountController : Controller
    {
        private readonly AuthService authService;
        private readonly GameStoreContext db;

        public AdminAccountController(AuthService _authService, GameStoreContext _db)
        {
            authService = _authService;
            db = _db;
        }

        private async Task<NguoiDung?> GetCurrentActiveAdminAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            var claim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentActiveAdminAsync();
            if (user == null)
                return Redirect("/auth/login");

            return View(user);
        }

        [HttpPost("update-name")]
        public async Task<IActionResult> UpdateName(string tenNguoiDung)
        {
            var user = await GetCurrentActiveAdminAsync();
            if (user == null)
                return Redirect("/auth/login");

            bool success = authService.UpdateName(user.MaNguoiDung, tenNguoiDung, out string msg);

            TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            var user = await GetCurrentActiveAdminAsync();
            if (user == null)
                return Redirect("/auth/login");

            bool success = authService.ChangePassword(user.MaNguoiDung, oldPass, newPass, confirmPass, out string msg);

            if (!success)
            {
                TempData["Msg"] = msg;
                return RedirectToAction("Profile");
            }

            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "Đổi mật khẩu admin thành công. Vui lòng đăng nhập lại 🔐";
            TempData["ToastType"] = "success";

            return Redirect("/auth/login");
        }
    }
}