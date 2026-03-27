using GameStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Controllers.Client.Library
{
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

            var game = db.ThuVienGames
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.MaGame == id);

            if (game == null)
                return NotFound();

            // 🔥 đánh dấu đã tải
            game.DaTai = true;
            db.SaveChanges();

            // 👉 redirect về link game (giả lập)
            return Redirect("/files/" + id + ".zip");
        }
    }
}
