using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GameStore.Controllers;

[Route("home")]
public class HomeController : Controller
{
    private readonly GameService gameService;
    private readonly CategoryService categoryService;

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
        int totalGames;

        // =========================
        // NEW / HOT CASE
        // =========================
        if (type == "new")
        {
            var all = gameService.GetNewGames();

            totalGames = all.Count;
            games = Paginate(all, page, pageSize);

            ViewBag.CategoryName = "🆕 New Games";
        }
        else if (type == "hot")
        {
            var all = gameService.GetHotGames();

            totalGames = all.Count;
            games = Paginate(all, page, pageSize);

            ViewBag.CategoryName = "🔥 Hot Games";
        }
        else
        {
            // FILTER + CACHE + DB
            games = gameService.FilterGames(search, category, page, pageSize);

            totalGames = gameService.CountGames(search, category);

            // Title
            if (!string.IsNullOrEmpty(search))
            {
                ViewBag.CategoryName = $"Search result: {search}";
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
        }

        // =========================
        // PAGINATION
        // =========================
        int totalPages = (int)Math.Ceiling((double)totalGames / pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Categories = categoryService.findAll();

        // =========================
        // USER DATA (Owned + Cart)
        // =========================
        List<string> ownedGameIds = new();
        List<string> cartGameIds = new();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                var db = gameService.GetDb();

                ownedGameIds = db.ChiTietGiaoDiches
                    .Where(x => x.GiaoDich.MaNguoiDung == userId)
                    .Select(x => x.MaGame)
                    .ToList();

                cartGameIds = db.ChiTietGioHangs
                    .Where(x => x.GioHang.MaNguoiDung == userId)
                    .Select(x => x.MaGame)
                    .ToList();
            }
        }

        ViewBag.OwnedGames = ownedGameIds;
        ViewBag.CartGames = cartGameIds;

        return View(games);
    }

    // =========================
    // HELPERS
    // =========================
    private List<Game> Paginate(List<Game> source, int page, int pageSize)
    {
        return source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
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