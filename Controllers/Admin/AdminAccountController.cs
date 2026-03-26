using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin/account")]
    public class AdminAccountController : Controller
    {
        private readonly AuthService authService;
        private readonly GameStoreContext db;

        public AdminAccountController(AuthService _authService, GameStoreContext _db)
        {
            authService = _authService;
            db = _db;
        }

        [HttpGet("profile")]
        public IActionResult Profile()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            int userId = int.Parse(User.FindFirst("UserId").Value);

            var user = db.NguoiDungs.Find(userId);

            return View(user);
        }

        [HttpPost("update-name")]
        public IActionResult UpdateName(string tenNguoiDung)
        {
            int userId = int.Parse(User.FindFirst("UserId").Value);

            authService.UpdateName(userId, tenNguoiDung, out string msg);

            TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }

        [HttpPost("change-password")]
        public IActionResult ChangePassword(string oldPass, string newPass, string confirmPass)
        {
            int userId = int.Parse(User.FindFirst("UserId").Value);

            authService.ChangePassword(userId, oldPass, newPass, confirmPass, out string msg);

            TempData["Msg"] = msg;

            return RedirectToAction("Profile");
        }
    }
}