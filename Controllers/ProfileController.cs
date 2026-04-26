using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    [Authorize]
    [Route("profile")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ProfileController : Controller
    {
        private readonly UserIconEffectService userIconEffectService;
        private readonly GameStoreContext db;

        public ProfileController(UserIconEffectService _userIconEffectService, GameStoreContext _db)
        {
            userIconEffectService = _userIconEffectService;
            db = _db;
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

        [Route("my-effects")]
        public async Task<IActionResult> MyEffects()
        {
            ViewBag.HideSubBar = true;

            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            var effects = userIconEffectService.GetByUser(user.MaNguoiDung);

            return View("~/Views/Profile/MyEffects.cshtml", effects);
        }

        [HttpPost]
        [Route("equip-effect/{id}")]
        public async Task<IActionResult> EquipEffect(int id)
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            if (userIconEffectService.Equip(user.MaNguoiDung, id))
            {
                TempData["ToastMessage"] = "Trang bị effect thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không thể trang bị effect.";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("MyEffects");
        }

        [HttpPost]
        [Route("unequip-effect")]
        public async Task<IActionResult> UnequipEffect()
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            if (userIconEffectService.Unequip(user.MaNguoiDung))
            {
                TempData["ToastMessage"] = "Đã gỡ effect hiện tại!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không thể gỡ effect.";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("MyEffects");
        }
    }
}