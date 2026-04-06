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
    public IActionResult Index(string search, string category, int page = 1)
    {
        int pageSize = 8;

        // =========================
        // 🔥 ALL GAME (DUY NHẤT có pagination)
        // =========================
        var games = gameService.FilterGames(search, category, page, pageSize);
        int totalGames = gameService.CountGames(search, category);

        // =========================
        // 🎯 TITLE
        // =========================
        if (!string.IsNullOrEmpty(search))
        {
            ViewBag.CategoryName = "Search: " + search;
        }
        else if (!string.IsNullOrEmpty(category))
        {
            var cate = categoryService.findAll()
                        .FirstOrDefault(c => c.MaTheLoai == category);

            ViewBag.CategoryName = cate?.TenLoaiGame ?? "Category";
        }
        else
        {
            ViewBag.CategoryName = "All Games";
        }

        // =========================
        // 🔥 PAGINATION (CHỈ ALL GAME)
        // =========================
        int totalPages = (int)Math.Ceiling((double)totalGames / pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        // =========================
        // 🎮 UI DATA (KHÔNG PAGINATE)
        // =========================
        ViewBag.HotTop = gameService.GetHotGames(3);                 // Top 3 → carousel
        ViewBag.NewGames = gameService.GetNewGames(10).Take(10).ToList(); // Scroll

        // =========================
        // 📂 CATEGORY (CHO FILTER UI)
        // =========================
        ViewBag.Categories = categoryService.findAll();

        // =========================
        // 🛒 OWNED + CART
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