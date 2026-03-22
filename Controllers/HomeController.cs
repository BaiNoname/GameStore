using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace GameStore.Controllers;

[Route("home")]
public class HomeController : Controller
{
    private readonly GameService gameService;
    private readonly CategoryService categoryService;
    private readonly IDistributedCache cache;

    public HomeController(
        GameService _gameService,
        CategoryService _categoryService,
        IDistributedCache _cache)
    {
        gameService = _gameService;
        categoryService = _categoryService;
        cache = _cache;
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
        // NEW / HOT
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
            games = gameService.FilterGames(search, category, page, pageSize);
            totalGames = gameService.CountGames(search, category);

            if (!string.IsNullOrEmpty(search))
                ViewBag.CategoryName = $"Search result: {search}";
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
        // USER CACHE (FIX Ở ĐÂY)
        // =========================
        List<string> ownedGameIds = new();
        List<string> cartGameIds = new();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                // ===== CACHE KEYS =====
                string ownedKey = $"owned_{userId}";
                string cartKey = $"cart_{userId}";

                // =========================
                // OWNED GAMES CACHE
                // =========================
                var ownedCached = cache.GetString(ownedKey);

                if (!string.IsNullOrEmpty(ownedCached))
                {
                    ownedGameIds = JsonSerializer.Deserialize<List<string>>(ownedCached);
                }
                else
                {
                    var db = gameService.GetDb();

                    ownedGameIds = db.ChiTietGiaoDiches
                        .Where(x => x.GiaoDich.MaNguoiDung == userId)
                        .Select(x => x.MaGame)
                        .ToList();

                    cache.SetString(
                        ownedKey,
                        JsonSerializer.Serialize(ownedGameIds),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                }

                // =========================
                // CART CACHE
                // =========================
                var cartCached = cache.GetString(cartKey);

                if (!string.IsNullOrEmpty(cartCached))
                {
                    cartGameIds = JsonSerializer.Deserialize<List<string>>(cartCached);
                }
                else
                {
                    var db = gameService.GetDb();

                    cartGameIds = db.ChiTietGioHangs
                        .Where(x => x.GioHang.MaNguoiDung == userId)
                        .Select(x => x.MaGame)
                        .ToList();

                    cache.SetString(
                        cartKey,
                        JsonSerializer.Serialize(cartGameIds),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                }
            }
        }

        ViewBag.OwnedGames = ownedGameIds;
        ViewBag.CartGames = cartGameIds;

        return View(games);
    }

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