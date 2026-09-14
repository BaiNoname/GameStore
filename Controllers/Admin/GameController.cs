using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.Controllers.Admin
{
    // Controller quản lý game trong trang admin, chỉ admin mới có quyền truy cập
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class GameController : Controller
    {
        private readonly GameStoreContext db;
        private readonly GameService gameService;
        private readonly CategoryService categoryService;
        private readonly IWebHostEnvironment env;
        private const int pageSize = 10;

        public GameController(GameStoreContext _db, GameService _gameService, CategoryService _categoryService, IWebHostEnvironment _env)
        {
            db = _db;
            gameService = _gameService;
            categoryService = _categoryService;
            env = _env;
        }

        // Hiển thị danh sách game với các bộ lọc và phân trang
        [Route("game/index")]
        public IActionResult Index(string keyword = "", string categoryId = "", int page = 1)
        {
            int totalPages;
            var games = gameService.findAll(keyword, categoryId, page, pageSize, out totalPages);

            var vm = new GameStore.Pagination.Admin.GameListVM
            {
                Games = games,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword,
                CategoryId = categoryId,
                Categories = categoryService.findAll()
            };

            return View("~/Views/Admin/Game/Index.cshtml", vm);
        }

        // Hiển thị form thêm game mới
        [Route("game/add")]
        public IActionResult Add(string keyword = "", string categoryId = "", int page = 1)
        {
            ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame");
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/Game/Add.cshtml");
        }

        // Xử lý thêm game mới, bao gồm upload hình ảnh và kiểm tra dữ liệu đầu vào
        [HttpPost]
        [Route("game/add")]
        public async Task<IActionResult> Add(Game game, IFormFile photo, string keyword = "", string categoryId = "", int page = 1)
        {
            // Trim các trường dữ liệu để tránh lỗi do khoảng trắng
            game.MaGame = game.MaGame?.Trim();
            game.TenGame = game.TenGame?.Trim();
            game.MaTheLoai = game.MaTheLoai?.Trim();

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(game.MaGame))
            {
                TempData["Msg"] = "❌ Mã game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            if (string.IsNullOrWhiteSpace(game.TenGame))
            {
                TempData["Msg"] = "❌ Tên game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            if (string.IsNullOrWhiteSpace(game.MaTheLoai))
            {
                TempData["Msg"] = "❌ Thể loại game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            if (game.Gia <= 0)
            {
                TempData["Msg"] = "❌ Giá game phải lớn hơn 0!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            if (game.NgayRaMat == default)
            {
                game.NgayRaMat = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            if (gameService.findById(game.MaGame) != null)
            {
                TempData["Msg"] = "❌ Mã game đã tồn tại!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            string fileName = null;
            try
            {
                // Gán giá trị mặc định cho lượt tải
                game.SoLuotTai = 0;

                // Xử lý upload hình ảnh nếu có
                if (photo != null && photo.Length > 0)
                {
                    var ext = Path.GetExtension(photo.FileName).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
                    {
                        TempData["Msg"] = "❌ Định dạng hình ảnh không hợp lệ!";
                        TempData["MsgType"] = "danger";
                        ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                        ViewBag.Keyword = keyword;
                        ViewBag.CategoryId = categoryId;
                        ViewBag.CurrentPage = page;
                        return View("~/Views/Admin/Game/Add.cshtml", game);
                    }

                    var uploadsFolder = Path.Combine(env.WebRootPath, "images");
                    fileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }

                    game.Hinh = fileName;
                }

                gameService.Create(game);

                TempData["Msg"] = "✅ Thêm game thành công!";
                TempData["MsgType"] = "success";

                return RedirectToAction("Index", new
                {
                    keyword,
                    categoryId,
                    page
                });
            }
            catch
            {
                // Nếu có lỗi xảy ra, xóa hình ảnh đã upload nếu có
                if (fileName != null)
                {
                    var path = Path.Combine(env.WebRootPath, "images", fileName);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                TempData["Msg"] = "❌ Thêm game thất bại!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }
        }

        // Hiển thị form chỉnh sửa game, nếu game không tồn tại sẽ hiển thị thông báo lỗi
        [Route("game/edit/{id}")]
        public IActionResult Edit(string id, string keyword = "", string categoryId = "", int page = 1)
        {
            // Tìm game theo ID, nếu không tồn tại thì hiển thị thông báo lỗi và chuyển hướng về trang danh sách
            var game = gameService.findById(id);
            if (game == null)
            {
                TempData["Msg"] = "❌ Game không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, categoryId, page });
            }

            ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/Game/Edit.cshtml", game);
        }

        // Xử lý chỉnh sửa game, bao gồm upload hình ảnh mới và xóa hình ảnh cũ nếu có
        [HttpPost]
        [Route("game/edit/{id}")]
        public async Task<IActionResult> Edit(Game game, IFormFile photo, string keyword = "", string categoryId = "", int page = 1)
        {
            // Tìm game cũ theo ID, nếu không tồn tại thì hiển thị thông báo lỗi và chuyển hướng về trang danh sách
            var oldGame = gameService.findById(game.MaGame);
            if (oldGame == null)
            {
                TempData["Msg"] = "❌ Game không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, categoryId, page });
            }

            // Trim các trường dữ liệu để tránh lỗi do khoảng trắng
            game.TenGame = game.TenGame?.Trim();
            game.MaTheLoai = game.MaTheLoai?.Trim();

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(game.TenGame))
            {
                TempData["Msg"] = "❌ Tên game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }

            if (string.IsNullOrWhiteSpace(game.MaTheLoai))
            {
                TempData["Msg"] = "❌ Thể loại game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }

            if (game.Gia <= 0)
            {
                TempData["Msg"] = "❌ Giá game phải lớn hơn 0!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }

            if (game.NgayRaMat == default)
            {
                game.NgayRaMat = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            // Biến lưu tên file mới nếu có, để xóa file cũ sau khi cập nhật thành công
            string newFileName = null;
            var oldImage = oldGame.Hinh;

            try
            {
                // Cập nhật các trường dữ liệu của game cũ bằng dữ liệu từ form
                oldGame.TenGame = game.TenGame;
                oldGame.MoTa = game.MoTa;
                oldGame.Gia = game.Gia;
                oldGame.MaTheLoai = game.MaTheLoai;
                oldGame.NgayRaMat = game.NgayRaMat;

                if (photo != null && photo.Length > 0)
                {
                    var ext = Path.GetExtension(photo.FileName).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
                    {
                        TempData["Msg"] = "❌ Định dạng hình ảnh không hợp lệ!";
                        TempData["MsgType"] = "danger";
                        ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                        ViewBag.Keyword = keyword;
                        ViewBag.CategoryId = categoryId;
                        ViewBag.CurrentPage = page;
                        return View("~/Views/Admin/Game/Edit.cshtml", game);
                    }

                    var uploadsFolder = Path.Combine(env.WebRootPath, "images");
                    newFileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadsFolder, newFileName);

                    // Upload hình ảnh mới
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }

                    oldGame.Hinh = newFileName;
                }

                gameService.Update(oldGame);

                // Nếu có hình ảnh mới và game cũ có hình ảnh, xóa hình ảnh cũ
                if (newFileName != null && !string.IsNullOrEmpty(oldImage))
                {
                    var oldPath = Path.Combine(env.WebRootPath, "images", oldImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                TempData["Msg"] = "✅ Chỉnh sửa game thành công!";
                TempData["MsgType"] = "success";

                return RedirectToAction("Index", new
                {
                    keyword,
                    categoryId,
                    page
                });
            }
            catch
            {
                // Nếu có lỗi xảy ra, xóa hình ảnh mới đã upload nếu có
                if (newFileName != null)
                {
                    var path = Path.Combine(env.WebRootPath, "images", newFileName);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                TempData["Msg"] = "❌ Chỉnh sửa game thất bại!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                ViewBag.Keyword = keyword;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }
        }

        // Xử lý xóa game, bao gồm xóa hình ảnh liên quan nếu có và hiển thị thông báo kết quả
        [Route("game/delete/{id}")]
        public IActionResult Delete(string id, string keyword = "", string categoryId = "", int page = 1)
        {
            // Tìm game theo ID, nếu không tồn tại thì hiển thị thông báo lỗi và chuyển hướng về trang danh sách
            var game = gameService.findById(id);
            // Nếu game tồn tại, xóa hình ảnh liên quan nếu có, sau đó xóa game và hiển thị thông báo kết quả
            if (game != null)
            {
                if (!string.IsNullOrEmpty(game.Hinh))
                {
                    var path = Path.Combine(env.WebRootPath, "images", game.Hinh);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                if (gameService.Delete(id))
                {
                    TempData["Msg"] = "✅ Xóa game thành công!";
                }
                else
                {
                    TempData["Msg"] = "❌ Xóa game thất bại!";
                }
            }
            else
            {
                TempData["Msg"] = "❌ Game không tồn tại!";
            }

            int totalItems = gameService.CountGames(keyword, categoryId);
            int maxPage = (int)Math.Ceiling((double)totalItems / pageSize);
            if (maxPage <= 0) maxPage = 1;
            if (page > maxPage) page = maxPage;

            return RedirectToAction("Index", new
            {
                keyword,
                categoryId,
                page
            });
        }
    }
}