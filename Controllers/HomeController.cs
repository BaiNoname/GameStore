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
        int totalGames = 0;

        // =========================
        // 🔥 CASE NEW / HOT
        // =========================
        if (type == "new")
        {
            var all = gameService.GetNewGames();
            totalGames = all.Count;

            games = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CategoryName = "🆕 New Games";
        }
        else if (type == "hot")
        {
            var all = gameService.GetHotGames();
            totalGames = all.Count;

            games = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CategoryName = "🔥 Hot Games";
        }
        else
        {
            // 🔥 DATA (đã có cache + pagination)
            games = gameService.FilterGames(search, category, page, pageSize);

            // 🔥 COUNT (phải query riêng)
            var query = gameService.GetDb().Games.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(g => g.TenGame.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.MaTheLoai == category);

            totalGames = gameService.CountGames(search, category);

            // 🎯 Title
            if (!string.IsNullOrEmpty(search))
            {
                ViewBag.CategoryName = "Search result: " + search;
            }
            else if (!string.IsNullOrEmpty(category))
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

        // =========================
        // 🔥 PAGINATION
        // =========================
        int totalPages = (int)Math.Ceiling((double)totalGames / pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Categories = categoryService.findAll();

        // =========================
        // 🔥 OWNED + CART
        // =========================
        List<string> ownedGameIds = new List<string>();
        List<string> cartGameIds = new List<string>();

        if (User.Identity.IsAuthenticated)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            ownedGameIds = gameService.GetDb().ThuVienGames
                .Where(x => x.MaNguoiDung == userId)
                .Select(x => x.MaGame)
                .ToList();

            cartGameIds = gameService.GetDb().ChiTietGioHangs
                .Where(x => x.GioHang.MaNguoiDung == userId)
                .Select(x => x.MaGame)
                .ToList();
        }

        ViewBag.OwnedGames = ownedGameIds;
        ViewBag.CartGames = cartGameIds;

        return View(games);
    }

    [Route("about")]
    public IActionResult About()
    {
        ViewBag.HideSubBar = true;
        return View();
    }

    [Route("support")]
    public IActionResult Support()
    {
        ViewBag.HideSubBar = true;
        return View();
    }


}