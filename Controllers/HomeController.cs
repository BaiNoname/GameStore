using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers;

[Route("home")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
    public IActionResult Index(string search, string globalSearch, string category, string layoutSearch, int page = 1)
    {
        int pageSize = 8;

        // =========================
        // 🔥 XÁC ĐỊNH KEYWORD ĐANG DÙNG
        // - search: ô search trong Home/Index
        // - globalSearch: ô search ở Layout
        // =========================
        string keyword = !string.IsNullOrWhiteSpace(globalSearch)
            ? globalSearch
            : search;

        // =========================
        // 🔥 ALL GAME (DUY NHẤT có pagination)
        // =========================
        var games = gameService.FilterGames(keyword, category, page, pageSize);
        int totalGames = gameService.CountGames(keyword, category);

        // =========================
        // 🎯 TITLE
        // =========================
        if (!string.IsNullOrEmpty(keyword))
        {
            ViewBag.CategoryName = "Search: " + keyword;
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
        ViewBag.HotTop = gameService.GetHotGames(5);
        ViewBag.NewGames = gameService.GetNewGames(10).Take(10).ToList();

        // =========================
        // 📂 CATEGORY (CHO FILTER UI)
        // =========================
        ViewBag.Categories = categoryService.findAll();

        // =========================
        // 🔍 GIỮ RIÊNG GIÁ TRỊ SEARCH
        // - searchValue: ô search trong Home/Index
        // - globalSearchValue: ô search ở Layout
        // - activeKeyword: keyword thực sự đang dùng để filter
        // - hideTopSections: chỉ ẩn Hot/New khi search từ layout
        // =========================
        ViewBag.SearchValue = search ?? "";
        ViewBag.GlobalSearchValue = globalSearch ?? "";
        ViewBag.ActiveKeyword = keyword ?? "";
        ViewBag.LayoutSearch = layoutSearch ?? "";
        ViewBag.HideTopSections =
            layoutSearch == "1" && !string.IsNullOrWhiteSpace(globalSearch);

        // =========================
        // 🛒 OWNED + CART
        // =========================
        List<string> ownedGameIds = new List<string>();
        List<string> cartGameIds = new List<string>();

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var claim = User.FindFirst("UserId")?.Value;

            if (int.TryParse(claim, out int userId))
            {
                var user = gameService.GetDb().NguoiDungs
                    .FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

                if (user != null)
                {
                    ownedGameIds = gameService.GetDb().ThuVienGames
                        .Where(x => x.MaNguoiDung == userId)
                        .Select(x => x.MaGame)
                        .ToList();

                    cartGameIds = gameService.GetDb().ChiTietGioHangs
                        .Where(x => x.GioHang.MaNguoiDung == userId)
                        .Select(x => x.MaGame)
                        .ToList();
                }
            }
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