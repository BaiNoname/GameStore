using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers;

[Route("home")]
public class HomeController : Controller
{
    private GameService gameService;
    private CategoryService categoryService;

    public HomeController(GameService _gameService, CategoryService _categoryService)
    {
        gameService = _gameService;
        categoryService = _categoryService;
    }


    [Route("~/")]
    [Route("index")]
    [Route("")]

    public IActionResult Index(string search, string category, string type)
    {
        List<Game> games;

        if (type == "new")
        {
            games = gameService.GetNewGames();
            ViewBag.CategoryName = "🆕 New Games";
        }
        else if (type == "hot")
        {
            games = gameService.GetHotGames();
            ViewBag.CategoryName = "🔥 Hot Games";
        }
        else if (!string.IsNullOrEmpty(category))
        {
            games = gameService.FilterGames(search, category);

            var cate = categoryService.findById(category);
            if (cate != null)
                ViewBag.CategoryName = "🎮 " + cate.TenLoaiGame;
            else
                ViewBag.CategoryName = "Games";
        }
        else if (!string.IsNullOrEmpty(search))
        {
            games = gameService.FilterGames(search, category);
            ViewBag.CategoryName = "🔍 Search: " + search;
        }
        else
        {
            games = gameService.FilterGames(search, category);
            ViewBag.CategoryName = "All Games";
        }

        ViewBag.Categories = categoryService.findAll();

        return View(games);
    }


}