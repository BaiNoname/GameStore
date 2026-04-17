using GameStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Controllers.Client.Library
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LibraryController: Controller
    {
        private readonly GameStoreContext db;

        public LibraryController(GameStoreContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            ViewBag.HideSubBar = true;

            if (!User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            int userId = int.Parse(User.FindFirst("UserId").Value);

            var data = db.ThuVienGames
                .Include(x => x.Game)
                .Where(x => x.MaNguoiDung == userId)
                .ToList();

            return View(data);
        }

        public IActionResult Download(string id)
        {
            int userId = int.Parse(User.FindFirst("UserId").Value);

            var item = db.ThuVienGames
                .Include(x => x.Game)
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == id);

            if (item == null)
                return NotFound();

            if (!item.DaTai)
            {
                item.DaTai = true;
            }


            db.SaveChanges();

            return Redirect(item.Game.LinkGame);
        }
    }
}
