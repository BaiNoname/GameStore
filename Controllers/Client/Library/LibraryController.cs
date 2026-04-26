using GameStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Controllers.Client.Library
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LibraryController : Controller
    {
        private readonly GameStoreContext db;

        public LibraryController(GameStoreContext _db)
        {
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

        public async Task<IActionResult> Index()
        {
            ViewBag.HideSubBar = true;

            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            var data = db.ThuVienGames
                .Include(x => x.Game)
                .Where(x => x.MaNguoiDung == user.MaNguoiDung)
                .ToList();

            return View(data);
        }

        public async Task<IActionResult> Download(string id)
        {
            var user = await GetCurrentActiveUserAsync();
            if (user == null)
                return Redirect("/auth/login");

            var item = db.ThuVienGames
                .Include(x => x.Game)
                .FirstOrDefault(x => x.MaNguoiDung == user.MaNguoiDung && x.MaGame == id);

            if (item == null)
                return NotFound();

            if (!item.DaTai)
            {
                item.DaTai = true;
                db.SaveChanges();
            }

            if (item.Game == null || string.IsNullOrWhiteSpace(item.Game.LinkGame))
                return NotFound();

            return Redirect(item.Game.LinkGame);
        }
    }
}