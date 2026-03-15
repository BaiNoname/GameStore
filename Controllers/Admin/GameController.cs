using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin")]
    public class GameController : Controller
    {
        private readonly GameStoreContext db;
        private GameService gameService;

        public GameController(GameStoreContext _db, GameService _gameService)
        {
            db = _db;
            gameService = _gameService;
        }

        [Route("game/index")]
        public IActionResult Index()
        {
            var games = db.Games.ToList();
            return View("~/Views/Admin/Game/Index.cshtml", games);
        }

        [Route("game/add")]
        public IActionResult Add()
        {

            return View("~/Views/Admin/Game/Add.cshtml");
        }

        [HttpPost]
        [Route("game/add")]
        public IActionResult Add(Game game)
        {
            if (gameService.Create(game))
            {
                TempData["Msg"] = "Add Succes";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Msg"] = "Add Failed";
                return RedirectToAction("Add");
            }
        }

    }
}
