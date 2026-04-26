using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Game
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
            ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Home");

            var game = gameService.findById(id);
            ViewBag.HideSubBar = true;

            ViewBag.Categories = categoryService.findAll();

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

                case "inactive_user":
                    TempData["ToastMessage"] = "Tài khoản của bạn không còn hoạt động.";
                    TempData["ToastType"] = "error";
                    break;

                default:
                    TempData["ToastMessage"] = "Không thể gửi đánh giá.";
                    TempData["ToastType"] = "error";
                    break;
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
