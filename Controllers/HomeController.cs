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

    public IActionResult Index(string search, string category, string type, int page = 1)
    {
        int pageSize = 5;

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
        else
        {
            games = gameService.FilterGames(search, category);

            if (!string.IsNullOrEmpty(category))
            {
                var cate = categoryService.findAll()
                            .FirstOrDefault(c => c.MaTheLoai == category);

                ViewBag.CategoryName = cate?.TenLoaiGame;
            }
            else
            {
                ViewBag.CategoryName = "All Games";
            }
        }

        int totalGames = games.Count();
        int totalPages = (int)Math.Ceiling((double)totalGames / pageSize);

        var pagedGames = games
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        ViewBag.Categories = categoryService.findAll();

        return View(pagedGames);
    }


}