using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Coupon
{
    // Trang khuyến mãi công khai: xem & "lấy mã"
    [Authorize]
    [Route("coupon")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class CouponController : Controller
    {
        private readonly CouponService couponService;
        private readonly GameStoreContext db;

        public CouponController(CouponService _couponService, GameStoreContext _db)
        {
            couponService = _couponService;
            db = _db;
        }

        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return null;
            var claim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId)) return null;

            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }
            return user;
        }

        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null) return Redirect("/auth/login");

            ViewBag.HideSubBar = true;
            ViewBag.ClaimedIds = couponService.GetClaimedIds(user.MaNguoiDung);

            var list = couponService.GetPublicList();
            return View(list);
        }

        // User bấm "Lấy mã"
        [HttpPost("claim")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(int maKM)
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null) return Redirect("/auth/login");

            var (ok, message) = couponService.Claim(user.MaNguoiDung, maKM);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return RedirectToAction("Index");
        }
    }
}
