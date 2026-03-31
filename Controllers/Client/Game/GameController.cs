using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Game
{
    public class GameController : Controller
    {
        private readonly GameService gameService;
        private CategoryService categoryService;
        private ReviewService reviewService;

        public GameController(GameService _gameService, CategoryService _categoryService, ReviewService _reviewService)
        {
            gameService = _gameService;
            categoryService = _categoryService;
            reviewService = _reviewService;
        }


        public IActionResult Detail(string id, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();

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
                ownedGameIds = gameService.GetDb().ThuVienGames
                    .Where(x => x.MaNguoiDung == userId)
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

            ViewBag.Reviews = reviewService.GetByGame(id);

            if (User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirst("UserId").Value);
                ViewBag.MyReview = reviewService.GetUserReview(userId, id);
            }

            return View(game);
        }

        [HttpPost]
        public IActionResult SubmitReview(string gameId, int rating, string comment)
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            int userId = int.Parse(User.FindFirst("UserId").Value);

            var result = reviewService.AddOrUpdate(userId, gameId, rating, comment);

            switch (result)
            {
                case "created":
                    TempData["ToastMessage"] = "Đánh giá thành công ⭐";
                    TempData["ToastType"] = "success";
                    break;

                case "updated":
                    TempData["ToastMessage"] = "Cập nhật đánh giá thành công 🔁";
                    TempData["ToastType"] = "success";
                    break;

                case "not_bought":
                    TempData["ToastMessage"] = "Bạn phải mua game trước khi đánh giá!";
                    TempData["ToastType"] = "error";
                    break;
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
