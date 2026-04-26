using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
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

        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
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

        public async Task<IActionResult> Profile()
        {
            ViewBag.HideSubBar = true;

            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            ViewBag.EquippedEffectCssClass = userIconEffectService.GetEquippedCssClass(user.MaNguoiDung);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateName(string tenNguoiDung)
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            bool result = authService.UpdateName(user.MaNguoiDung, tenNguoiDung, out string msg);

            if (!result)
                TempData["Err"] = msg;
            else
                TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            bool success = authService.ChangePassword(user.MaNguoiDung, oldPass, newPass, confirmPass, out string message);

            if (!success)
            {
                TempData["Err"] = message;
                return RedirectToAction("Profile");
            }

            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại 🔐";
            TempData["ToastType"] = "success";

            return RedirectToAction("Login", "Auth");
        }
    }
}