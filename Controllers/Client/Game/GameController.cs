using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Game
{
    // Controller để hiển thị chi tiết game và xử lý đánh giá của người dùng
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

        // Hiển thị trang chi tiết game
        public IActionResult Detail(string id, string returnUrl)
        {
            ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Home");

            var game = gameService.findById(id);
            ViewBag.HideSubBar = true;

            ViewBag.Categories = categoryService.findAll();

            // Lấy danh sách game mà người dùng đã sở hữu và đã thêm vào giỏ hàng
            List<string> ownedGameIds = new List<string>();
            List<string> cartGameIds = new List<string>();

            // Chỉ lấy thông tin này nếu người dùng đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst("UserId").Value);

                // Lấy danh sách mã game mà người dùng đã sở hữu
                ownedGameIds = gameService.GetDb().ThuVienGames
                    .Where(x => x.MaNguoiDung == userId)
                    .Select(x => x.MaGame)
                    .ToList();

                // Lấy danh sách mã game mà người dùng đã thêm vào giỏ hàng
                cartGameIds = gameService.GetDb().ChiTietGioHangs
                    .Where(x => x.GioHang.MaNguoiDung == userId)
                    .Select(x => x.MaGame)
                    .ToList();
            }

            ViewBag.OwnedGames = ownedGameIds;
            ViewBag.CartGames = cartGameIds;

            // Lấy danh sách đánh giá của game
            ViewBag.Reviews = reviewService.GetByGame(id);

            // Nếu người dùng đã đăng nhập, lấy đánh giá của họ cho game này (nếu có)
            if (User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirst("UserId").Value);
                ViewBag.MyReview = reviewService.GetUserReview(userId, id);
            }

            return View(game);
        }

        // Xử lý khi người dùng gửi đánh giá cho game
        [HttpPost]
        public IActionResult SubmitReview(string gameId, int rating, string comment)
        {
            // Kiểm tra nếu người dùng chưa đăng nhập thì chuyển hướng đến trang đăng nhập
            if (!User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            // Lấy userId từ claims của người dùng
            int userId = int.Parse(User.FindFirst("UserId").Value);

            // Gọi service để thêm hoặc cập nhật đánh giá
            var result = reviewService.AddOrUpdate(userId, gameId, rating, comment);

            // Hiển thị thông báo toast dựa trên kết quả trả về từ service
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
