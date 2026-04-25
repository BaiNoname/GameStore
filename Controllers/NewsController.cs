using GameStore.Pagination.User;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    [Route("news")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class NewsController : Controller
    {
        private readonly NewsService newsService;

        public NewsController(NewsService _newsService)
        {
            newsService = _newsService;
        }

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

        [Route("detail/{slug}")]
        public IActionResult Detail(string slug, string newsType = "All", int page = 1)
        {
            ViewBag.HideSubBar = true;

            if (string.IsNullOrWhiteSpace(slug))
                return RedirectToAction("Index", new { newsType, page });

            var news = newsService.FindBySlug(slug);

            if (news == null)
            {
                TempData["ToastMessage"] = "Bài viết không tồn tại hoặc đã hết hạn";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", new { newsType, page });
            }

            newsService.IncreaseView(news.NewsId);

            ViewBag.TrendingNews = newsService.GetTrending(4);
            ViewBag.LatestNews = newsService.GetLatest(4);
            ViewBag.NewsType = string.IsNullOrWhiteSpace(newsType) ? "All" : newsType;
            ViewBag.Page = page;

            return View("~/Views/News/Detail.cshtml", news);
        }
    }
}