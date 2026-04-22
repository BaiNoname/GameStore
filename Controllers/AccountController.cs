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

        public IActionResult Profile()
        {
            ViewBag.HideSubBar = true;

            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var user = db.NguoiDungs.Find(userId);
            if (user == null)
                return Redirect("/auth/login");

            ViewBag.EquippedEffectCssClass = userIconEffectService.GetEquippedCssClass(userId);

            return View(user);
        }

        [HttpPost]
        public IActionResult UpdateName(string tenNguoiDung)
        {
            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            bool result = authService.UpdateName(userId, tenNguoiDung, out string msg);

            if (!result)
                TempData["Err"] = msg;
            else
                TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            bool success = authService.ChangePassword(userId, oldPass, newPass, confirmPass, out string message);

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