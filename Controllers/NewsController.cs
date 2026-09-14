using GameStore.Pagination.User;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    // Controller để xử lý các yêu cầu liên quan đến tin tức
    [Route("news")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class NewsController : Controller
    {
        private readonly NewsService newsService;

        public NewsController(NewsService _newsService)
        {
            newsService = _newsService;
        }

        // Hiển thị trang danh sách tin tức với phân trang và lọc theo loại tin
        [Route("")]
        [Route("index")]
        public IActionResult Index(string newsType = "All", int page = 1)
        {
            ViewBag.HideSubBar = true;

            int pageSize = 6;
            int totalPages;

            var featured = newsService.GetFeatured(1);
            var latest = newsService.FindPublished(newsType, page, pageSize, out totalPages);
            var trending = newsService.GetTrending(4);

            // Tạo ViewModel để truyền dữ liệu đến view
            var vm = new NewsPageVM
            {
                FeaturedNews = featured,
                LatestNews = latest,
                TrendingNews = trending,
                NewsType = string.IsNullOrWhiteSpace(newsType) ? "All" : newsType,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View("~/Views/News/Index.cshtml", vm);
        }

        // Hiển thị trang chi tiết của một tin tức dựa trên slug
        [Route("detail/{slug}")]
        public IActionResult Detail(string slug, string newsType = "All", int page = 1)
        {
            ViewBag.HideSubBar = true;

            // Nếu slug không hợp lệ, chuyển hướng về trang danh sách tin tức
            if (string.IsNullOrWhiteSpace(slug))
                return RedirectToAction("Index", new { newsType, page });

            // Tìm tin tức theo slug
            var news = newsService.FindBySlug(slug);

            // Nếu tin tức không tồn tại hoặc đã hết hạn, hiển thị thông báo lỗi và chuyển hướng về trang danh sách
            if (news == null)
            {
                TempData["ToastMessage"] = "Bài viết không tồn tại hoặc đã hết hạn";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", new { newsType, page });
            }

            // Tăng số lượt xem của tin tức
            newsService.IncreaseView(news.NewsId);

            // Lấy danh sách tin tức nổi bật, mới nhất và thịnh hành để hiển thị ở sidebar
            ViewBag.TrendingNews = newsService.GetTrending(4);
            ViewBag.LatestNews = newsService.GetLatest(4);
            ViewBag.NewsType = string.IsNullOrWhiteSpace(newsType) ? "All" : newsType;
            ViewBag.Page = page;

            return View("~/Views/News/Detail.cshtml", news);
        }
    }
}