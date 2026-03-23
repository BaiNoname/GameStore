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


        public IActionResult Detail(string id, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            var game = gameService.findById(id);
            ViewBag.HideSubBar = true;

            ViewBag.Categories = categoryService.findAll();

            // 🔥 THÊM ĐOẠN NÀY (GIỐNG HOME)
            List<string> ownedGameIds = new List<string>();
            List<string> cartGameIds = new List<string>();

            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst("UserId").Value);

                // 🎮 game đã mua
                ownedGameIds = gameService.GetDb().ChiTietGiaoDiches
                    .Where(x => x.GiaoDich.MaNguoiDung == userId)
                    .Select(x => x.MaGame)
                    .ToList();

                // 🛒 game trong cart
                cartGameIds = gameService.GetDb().ChiTietGioHangs
                    .Where(x => x.GioHang.MaNguoiDung == userId)
                    .Select(x => x.MaGame)
                    .ToList();
            }

            ViewBag.OwnedGames = ownedGameIds;
            ViewBag.CartGames = cartGameIds;

            return View(game);
        }
    }
}
