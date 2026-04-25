using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class NewsController : Controller
    {
        private readonly NewsService newsService;
        private readonly GameService gameService;
        private readonly IWebHostEnvironment env;
        private const int pageSize = 10;

        public NewsController(NewsService _newsService, GameService _gameService, IWebHostEnvironment _env)
        {
            newsService = _newsService;
            gameService = _gameService;
            env = _env;
        }

        private void LoadGameSelectList(string? selectedGameId = null)
        {
            ViewBag.Games = new SelectList(
                gameService.GetDb().Games.OrderBy(x => x.TenGame).ToList(),
                "MaGame",
                "TenGame",
                selectedGameId
            );
        }

        private string? SaveNewsImage(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0)
                return null;

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (!allowed.Contains(ext))
                return null;

            var folder = Path.Combine(env.WebRootPath, "images", "news");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            return fileName;
        }

        private void DeleteNewsImage(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var path = Path.Combine(env.WebRootPath, "images", "news", fileName);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        [Route("news/index")]
        public IActionResult Index(string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            int totalPages;
            var newsList = newsService.FindAll(keyword, newsType, status, page, pageSize, out totalPages);

            ViewBag.Keyword = keyword;
            ViewBag.NewsType = newsType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("~/Views/Admin/News/Index.cshtml", newsList);
        }

        [Route("news/add")]
        public IActionResult Add(string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            LoadGameSelectList();

            ViewBag.Keyword = keyword;
            ViewBag.NewsType = newsType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            var nowUtc = DateTime.UtcNow;

            return View("~/Views/Admin/News/Add.cshtml", new Models.News
            {
                PublishedAt = nowUtc,
                ExpiredAt = nowUtc.AddMonths(1),
                NewsType = "General",
                Status = "Published"
            });
        }

        [HttpPost]
        [Route("news/add")]
        public IActionResult Add(Models.News news, IFormFile? photo, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            news.Title = news.Title?.Trim() ?? "";
            news.Slug = news.Slug?.Trim().ToLower() ?? "";

            if (!User.Identity!.IsAuthenticated)
                return Redirect("/auth/login");

            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            news.AuthorUserId = userId;

            LoadGameSelectList(news.RelatedGameId);

            ViewBag.Keyword = keyword;
            ViewBag.NewsType = newsType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            var postedNewsType = Request.Form["NewsType"].ToString();
            var postedStatus = Request.Form["Status"].ToString();

            news.NewsType = string.IsNullOrWhiteSpace(postedNewsType) ? "General" : postedNewsType.Trim();
            news.Status = string.IsNullOrWhiteSpace(postedStatus) ? "Published" : postedStatus.Trim();

            if (string.IsNullOrWhiteSpace(news.Title))
            {
                TempData["Msg"] = "❌ Tiêu đề không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            if (string.IsNullOrWhiteSpace(news.Slug))
            {
                TempData["Msg"] = "❌ Slug không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            if (string.IsNullOrWhiteSpace(news.Content))
            {
                TempData["Msg"] = "❌ Nội dung không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            var existedSlug = newsService.FindAll("", "", "", 1, int.MaxValue, out int _)
                .FirstOrDefault(x => x.Slug == news.Slug);

            if (existedSlug != null)
            {
                TempData["Msg"] = "❌ Slug đã tồn tại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            var publishedAtRaw = Request.Form["PublishedAt"].ToString();
            var expiredAtRaw = Request.Form["ExpiredAt"].ToString();

            if (!DateTime.TryParse(publishedAtRaw, out DateTime publishedAtLocal))
            {
                TempData["Msg"] = "❌ Published At không hợp lệ!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            if (!DateTime.TryParse(expiredAtRaw, out DateTime expiredAtLocal))
            {
                TempData["Msg"] = "❌ Expired At không hợp lệ!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            if (expiredAtLocal <= publishedAtLocal)
            {
                TempData["Msg"] = "❌ Expired At phải lớn hơn Published At!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Add.cshtml", news);
            }

            news.PublishedAt = publishedAtLocal;
            news.ExpiredAt = expiredAtLocal;

            if (photo != null)
            {
                var savedFile = SaveNewsImage(photo);
                if (savedFile == null)
                {
                    TempData["Msg"] = "❌ Ảnh không hợp lệ! Chỉ chấp nhận .jpg, .jpeg, .png, .webp, .gif";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Add.cshtml", news);
                }

                news.Thumbnail = savedFile;
            }

            if (newsService.Create(news))
            {
                TempData["Msg"] = "✅ Thêm bài viết thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, newsType, status, page });
            }

            TempData["Msg"] = "❌ Thêm bài viết thất bại!";
            TempData["MsgType"] = "danger";
            return View("~/Views/Admin/News/Add.cshtml", news);
        }

        [Route("news/edit/{id}")]
        public IActionResult Edit(int id, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            var news = newsService.FindById(id);
            if (news == null)
            {
                TempData["Msg"] = "❌ Bài viết không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, newsType, status, page });
            }

            LoadGameSelectList(news.RelatedGameId);

            ViewBag.Keyword = keyword;
            ViewBag.NewsType = newsType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/News/Edit.cshtml", news);
        }

        [HttpPost]
        [Route("news/edit/{id}")]
        public IActionResult Edit(int id, Models.News news, IFormFile? photo, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            var current = newsService.FindById(id);
            if (current == null)
            {
                TempData["Msg"] = "❌ Bài viết không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, newsType, status, page });
            }

            LoadGameSelectList(current.RelatedGameId);

            ViewBag.Keyword = keyword;
            ViewBag.NewsType = newsType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            var postedNewsType = Request.Form["NewsType"].ToString();
            var postedStatus = Request.Form["Status"].ToString();

            news.NewsId = id;
            news.Title = news.Title?.Trim() ?? "";
            news.Slug = news.Slug?.Trim().ToLower() ?? "";
            news.NewsType = string.IsNullOrWhiteSpace(postedNewsType) ? current.NewsType : postedNewsType.Trim();
            news.Status = string.IsNullOrWhiteSpace(postedStatus) ? current.Status : postedStatus.Trim();

            if (string.IsNullOrWhiteSpace(news.Title))
            {
                TempData["Msg"] = "❌ Tiêu đề không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Edit.cshtml", current);
            }

            if (string.IsNullOrWhiteSpace(news.Slug))
            {
                TempData["Msg"] = "❌ Slug không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Edit.cshtml", current);
            }

            if (string.IsNullOrWhiteSpace(news.Content))
            {
                TempData["Msg"] = "❌ Nội dung không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Edit.cshtml", current);
            }

            var duplicateSlug = newsService.FindAll("", "", "", 1, int.MaxValue, out int _)
                .FirstOrDefault(x => x.Slug == news.Slug && x.NewsId != news.NewsId);

            if (duplicateSlug != null)
            {
                TempData["Msg"] = "❌ Slug đã tồn tại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/News/Edit.cshtml", current);
            }

            // Giữ PublishedAt cũ, không cho edit lại
            news.PublishedAt = current.PublishedAt;

            // ExpiredAt: nếu giữ nguyên giá trị cũ thì không coi là đang sửa
            var expiredAtRaw = Request.Form["ExpiredAt"].ToString();
            var currentExpiredRaw = current.ExpiredAt.HasValue
                ? current.ExpiredAt.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm")
                : "";

            if (string.IsNullOrWhiteSpace(expiredAtRaw) || expiredAtRaw == currentExpiredRaw)
            {
                news.ExpiredAt = current.ExpiredAt;
            }
            else
            {
                if (!DateTime.TryParse(expiredAtRaw, out DateTime parsedExpiredAt))
                {
                    TempData["Msg"] = "❌ Expired At không hợp lệ!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Edit.cshtml", current);
                }

                if (parsedExpiredAt <= current.PublishedAt.ToLocalTime())
                {
                    TempData["Msg"] = "❌ Expired At phải lớn hơn Published At!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Edit.cshtml", current);
                }

                news.ExpiredAt = parsedExpiredAt;
            }

            if (photo != null && photo.Length > 0)
            {
                var savedFile = SaveNewsImage(photo);
                if (savedFile == null)
                {
                    TempData["Msg"] = "❌ Ảnh không hợp lệ! Chỉ chấp nhận .jpg, .jpeg, .png, .webp, .gif";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Edit.cshtml", current);
                }

                DeleteNewsImage(current.Thumbnail);
                news.Thumbnail = savedFile;
            }
            else
            {
                news.Thumbnail = current.Thumbnail;
            }

            news.AuthorUserId = current.AuthorUserId;
            news.ViewCount = current.ViewCount;
            news.CreatedAt = current.CreatedAt;

            if (newsService.Update(news))
            {
                TempData["Msg"] = "✅ Cập nhật bài viết thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, newsType, status, page });
            }

            TempData["Msg"] = "❌ Cập nhật bài viết thất bại!";
            TempData["MsgType"] = "danger";
            return View("~/Views/Admin/News/Edit.cshtml", current);
        }

        [Route("news/delete/{id}")]
        public IActionResult Delete(int id, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            var news = newsService.FindById(id);

            if (news != null)
            {
                DeleteNewsImage(news.Thumbnail);
            }

            if (newsService.Delete(id))
            {
                TempData["Msg"] = "✅ Xóa bài viết thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Xóa bài viết thất bại!";
                TempData["MsgType"] = "danger";
            }

            int totalPages;
            newsService.FindAll(keyword, newsType, status, 1, pageSize, out totalPages);

            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            return RedirectToAction("Index", new { keyword, newsType, status, page });
        }
    }
}