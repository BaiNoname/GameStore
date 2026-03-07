using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Game
{
    public class GameController : Controller
    {
        private readonly GameService gameService;

        public GameController(GameService _gameService)
        {
            gameService = _gameService;
        }

        [HttpGet]
        public IActionResult Detail(string id)
        {
            var game = gameService.findById(id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
    }
}
