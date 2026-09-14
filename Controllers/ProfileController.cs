using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    // Controller xử lý các chức năng liên quan đến profile người dùng, đặc biệt là quản lý effect cho icon người dùng
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

        // Phương thức tiện ích để lấy thông tin người dùng hiện tại và kiểm tra xem họ còn hoạt động hay không
        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            // Lấy claim chứa UserId từ token hoặc cookie
            var claim = User.FindFirst("UserId")?.Value;

            // Nếu claim không tồn tại hoặc không thể chuyển đổi thành số nguyên, trả về null
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            // Truy vấn cơ sở dữ liệu để lấy thông tin người dùng dựa trên UserId và kiểm tra xem họ có còn hoạt động hay không
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

            // Nếu người dùng không tồn tại hoặc không còn hoạt động, xóa session và đăng xuất
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

        // Hiển thị trang quản lý effect của người dùng
        [Route("my-effects")]
        public async Task<IActionResult> MyEffects()
        {
            ViewBag.HideSubBar = true;

            // Lấy thông tin người dùng hiện tại và kiểm tra xem họ còn hoạt động hay không
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Lấy danh sách effect của người dùng từ dịch vụ
            var effects = userIconEffectService.GetByUser(user.MaNguoiDung);

            return View("~/Views/Profile/MyEffects.cshtml", effects);
        }

        // Xử lý yêu cầu trang bị effect cho icon người dùng
        [HttpPost]
        [Route("equip-effect/{id}")]
        public async Task<IActionResult> EquipEffect(int id)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem họ còn hoạt động hay không
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Gọi dịch vụ để trang bị effect cho người dùng và hiển thị thông báo tương ứng
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

        // Xử lý yêu cầu gỡ bỏ effect khỏi icon người dùng
        [HttpPost]
        [Route("unequip-effect")]
        public async Task<IActionResult> UnequipEffect()
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem họ còn hoạt động hay không
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            // Gọi dịch vụ để gỡ bỏ effect khỏi người dùng và hiển thị thông báo tương ứng
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