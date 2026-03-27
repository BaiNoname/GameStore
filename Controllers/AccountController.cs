using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService authService;
        private readonly GameStoreContext db;

        public AccountController(AuthService _authService, GameStoreContext _db)
        {
            authService = _authService;
            db = _db;
        }

        // ================= PROFILE =================
        public IActionResult Profile()
        {
            ViewBag.HideSubBar = true;
            if (!User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            int userId = int.Parse(User.FindFirst("UserId").Value);

            var user = db.NguoiDungs.Find(userId); // 🔥 lấy user

            if (user == null)
                return Redirect("/auth/login");

            return View(user); // 🔥 TRUYỀN MODEL
        }

        // ================= UPDATE NAME =================
        [HttpPost]
        public IActionResult UpdateName(string tenNguoiDung)
        {
            int userId = int.Parse(User.FindFirst("UserId").Value);

            bool result = authService.UpdateName(userId, tenNguoiDung, out string msg);

            if (!result)
                TempData["Err"] = msg;
            else
                TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }

        // ================= CHANGE PASSWORD =================
        [HttpPost]
        public IActionResult ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            int userId = int.Parse(User.FindFirst("UserId").Value);

            bool result = authService.ChangePassword(userId, oldPass, newPass, confirmPass, out string msg);

            if (!result)
                TempData["Err"] = msg;
            else
                TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }
    }
}