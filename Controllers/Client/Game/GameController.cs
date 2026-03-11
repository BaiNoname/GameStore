using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Game
{
    public class GameController : Controller
    {
        private readonly GameService gameService;
        private CategoryService categoryService;

        public GameController(GameService _gameService, CategoryService _categoryService)
        {
            gameService = _gameService;
            categoryService = _categoryService;
        }


        public IActionResult Detail(string id)
        {
            var game = gameService.findById(id);
            ViewBag.HideSubBar = true;

            ViewBag.Categories = categoryService.findAll();

            return View(game);
        }
    }
}
