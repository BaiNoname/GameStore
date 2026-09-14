using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.Controllers.Admin
{
    /// Controller quản lý tin tức, chỉ admin mới có quyền truy cập
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

        // Đảm bảo giá trị DateTime được lưu dưới dạng UTC
        private DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            if (value.Kind == DateTimeKind.Local)
                return value.ToUniversalTime();

            return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
        }

        // Chuyển đổi DateTime từ UTC sang giờ địa phương để hiển thị
        private DateTime ToLocalDisplay(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value.ToLocalTime();

            if (value.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();

            return value.ToLocalTime();
        }

        // Tải danh sách game để hiển thị trong dropdown khi tạo/sửa tin tức
        private void LoadGameSelectList(string? selectedGameId = null)
        {
            ViewBag.Games = new SelectList(
                gameService.GetDb().Games.OrderBy(x => x.TenGame).ToList(),
                "MaGame",
                "TenGame",
                selectedGameId
            );
        }

        // Lưu ảnh tin tức lên server và trả về tên file đã lưu, hoặc null nếu có lỗi
        private string? SaveNewsImage(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0)
                return null;

            // Chỉ chấp nhận các định dạng ảnh phổ biến
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (!allowed.Contains(ext))
                return null;

            // Tạo thư mục lưu ảnh nếu chưa tồn tại
            var folder = Path.Combine(env.WebRootPath, "images", "news");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Tạo tên file ngẫu nhiên để tránh trùng lặp
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            return fileName;
        }

        // Xóa ảnh tin tức khỏi server khi bài viết bị xóa hoặc ảnh bị thay đổi
        private void DeleteNewsImage(string? fileName)
        {
            // Nếu fileName null hoặc rỗng thì không làm gì
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            // Đảm bảo chỉ xóa file trong thư mục images/news để tránh lỗi bảo mật
            var path = Path.Combine(env.WebRootPath, "images", "news", fileName);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        // Danh sách tin tức với phân trang và bộ lọc
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

        // Trang tạo tin tức mới
        [Route("news/add")]
        public IActionResult Add(string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            LoadGameSelectList();

            ViewBag.Keyword = keyword;
            ViewBag.NewsType = newsType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            var nowUtc = DateTime.UtcNow;

            // Thiết lập giá trị mặc định cho PublishedAt là thời điểm hiện tại và ExpiredAt là 1 tháng sau
            return View("~/Views/Admin/News/Add.cshtml", new Models.News
            {
                PublishedAt = nowUtc,
                ExpiredAt = nowUtc.AddMonths(1),
                NewsType = "General",
                Status = "Published"
            });
        }

        // Xử lý POST khi tạo tin tức mới
        [HttpPost]
        [Route("news/add")]
        public IActionResult Add(Models.News news, IFormFile? photo, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            // Chuẩn hóa dữ liệu đầu vào
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

            // Lấy giá trị NewsType và Status từ form, nếu không có thì dùng mặc định
            var postedNewsType = Request.Form["NewsType"].ToString();
            var postedStatus = Request.Form["Status"].ToString();

            // Nếu người dùng không chọn NewsType hoặc Status thì mặc định là "General" và "Published"
            news.NewsType = string.IsNullOrWhiteSpace(postedNewsType) ? "General" : postedNewsType.Trim();
            news.Status = string.IsNullOrWhiteSpace(postedStatus) ? "Published" : postedStatus.Trim();

            // Kiểm tra các trường bắt buộc
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

            // Chuyển đổi thời gian sang UTC trước khi lưu vào database
            news.PublishedAt = EnsureUtc(publishedAtLocal);
            news.ExpiredAt = EnsureUtc(expiredAtLocal);

            if (photo != null)
            {
                // Lưu ảnh lên server và lấy tên file đã lưu
                var savedFile = SaveNewsImage(photo);
                if (savedFile == null)
                {
                    TempData["Msg"] = "❌ Ảnh không hợp lệ! Chỉ chấp nhận .jpg, .jpeg, .png, .webp, .gif";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Add.cshtml", news);
                }

                // Gán tên file ảnh vào trường Thumbnail của tin tức
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

        // Trang chỉnh sửa tin tức
        [Route("news/edit/{id}")]
        public IActionResult Edit(int id, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            // Tìm tin tức theo id, nếu không tồn tại thì hiển thị thông báo lỗi và chuyển về trang danh sách
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

            // Chuyển đổi thời gian từ UTC sang giờ địa phương để hiển thị trên form chỉnh sửa
            news.PublishedAt = ToLocalDisplay(news.PublishedAt);
            if (news.ExpiredAt.HasValue)
                news.ExpiredAt = ToLocalDisplay(news.ExpiredAt.Value);

            return View("~/Views/Admin/News/Edit.cshtml", news);
        }

        // Xử lý POST khi chỉnh sửa tin tức
        [HttpPost]
        [Route("news/edit/{id}")]
        public IActionResult Edit(int id, Models.News news, IFormFile? photo, string keyword = "", string newsType = "", string status = "", int page = 1)
        {
            // Tìm tin tức theo id, nếu không tồn tại thì hiển thị thông báo lỗi và chuyển về trang danh sách
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

            // Chuẩn hóa dữ liệu đầu vào
            var postedNewsType = Request.Form["NewsType"].ToString();
            var postedStatus = Request.Form["Status"].ToString();

            // Gán lại các trường cần thiết cho đối tượng news để cập nhật
            news.NewsId = id;
            news.Title = news.Title?.Trim() ?? "";
            news.Slug = news.Slug?.Trim().ToLower() ?? "";
            news.NewsType = string.IsNullOrWhiteSpace(postedNewsType) ? current.NewsType : postedNewsType.Trim();
            news.Status = string.IsNullOrWhiteSpace(postedStatus) ? current.Status : postedStatus.Trim();

            // Kiểm tra các trường bắt buộc
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

            // Đối với các trường ngày tháng, nếu người dùng không nhập gì thì giữ nguyên giá trị cũ, nếu có nhập thì kiểm tra hợp lệ và chuyển sang UTC
            news.PublishedAt = current.PublishedAt;

            var expiredAtRaw = Request.Form["ExpiredAt"].ToString();
            var currentExpiredRaw = current.ExpiredAt.HasValue
                ? ToLocalDisplay(current.ExpiredAt.Value).ToString("yyyy-MM-ddTHH:mm")
                : "";

            // Nếu người dùng không nhập gì cho ExpiredAt thì giữ nguyên giá trị cũ, nếu có nhập thì kiểm tra hợp lệ và chuyển sang UTC
            if (string.IsNullOrWhiteSpace(expiredAtRaw) || expiredAtRaw == currentExpiredRaw)
            {
                news.ExpiredAt = current.ExpiredAt;
            }
            else
            {
                if (!DateTime.TryParse(expiredAtRaw, out DateTime parsedExpiredAtLocal))
                {
                    TempData["Msg"] = "❌ Expired At không hợp lệ!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Edit.cshtml", current);
                }

                if (parsedExpiredAtLocal <= ToLocalDisplay(current.PublishedAt))
                {
                    TempData["Msg"] = "❌ Expired At phải lớn hơn Published At!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/News/Edit.cshtml", current);
                }

                news.ExpiredAt = EnsureUtc(parsedExpiredAtLocal);
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

            // Các trường không cho phép chỉnh sửa sẽ giữ nguyên giá trị cũ
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

        // Xóa tin tức
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