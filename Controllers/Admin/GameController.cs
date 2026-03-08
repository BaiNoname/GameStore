using GameStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin")]
    public class GameController : Controller
    {
        private readonly GameStoreContext db;

        public GameController(GameStoreContext _db)
        {
            db = _db;
        }

        [Route("game/index")]
        public IActionResult Index()
        {
            var games = db.Games.ToList();
            return View("~/Views/Admin/Game/Index.cshtml", games);
        }
    }
}
