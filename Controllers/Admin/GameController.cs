using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
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

        [Route("game/add")]
        public IActionResult Add()
        {
            ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame");
            return View("~/Views/Admin/Game/Add.cshtml");
        }

        [HttpPost]
        [Route("game/add")]
        public async Task<IActionResult> Add(Game game, IFormFile photo)
        {
            // Trim các field chuỗi
            game.MaGame = game.MaGame?.Trim();
            game.TenGame = game.TenGame?.Trim();
            game.MaTheLoai = game.MaTheLoai?.Trim();

            // Validation bắt buộc
            if (string.IsNullOrWhiteSpace(game.MaGame))
            {
                TempData["Msg"] = "❌ Mã game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }
            if (string.IsNullOrWhiteSpace(game.TenGame))
            {
                TempData["Msg"] = "❌ Tên game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }
            if (string.IsNullOrWhiteSpace(game.MaTheLoai))
            {
                TempData["Msg"] = "❌ Thể loại game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }
            if (game.Gia <= 0)
            {
                TempData["Msg"] = "❌ Giá game phải lớn hơn 0!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            // Ngày phát hành mặc định = ngày hiện tại nếu chưa nhập
            if (game.NgayRaMat == default)
            {
                game.NgayRaMat = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            // Check trùng MaGame
            if (gameService.findById(game.MaGame) != null)
            {
                TempData["Msg"] = "❌ Mã game đã tồn tại!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }

            string fileName = null;
            try
            {
                game.SoLuotTai = 0;

                // Xử lý ảnh nếu có
                if (photo != null && photo.Length > 0)
                {
                    var ext = Path.GetExtension(photo.FileName).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
                    {
                        TempData["Msg"] = "❌ Định dạng hình ảnh không hợp lệ!";
                        TempData["MsgType"] = "danger";
                        ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
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
                return RedirectToAction("Index");
            }
            catch
            {
                if (fileName != null)
                {
                    var path = Path.Combine(env.WebRootPath, "images", fileName);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                TempData["Msg"] = "❌ Thêm game thất bại!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Add.cshtml", game);
            }
        }

        [Route("game/edit/{id}")]
        public IActionResult Edit(string id, int page = 1)
        {
            var game = gameService.findById(id);
            if (game == null)
            {
                TempData["Msg"] = "❌ Game không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
            ViewBag.CurrentPage = page;
            return View("~/Views/Admin/Game/Edit.cshtml", game);
        }

        [HttpPost]
        [Route("game/edit/{id}")]
        public async Task<IActionResult> Edit(Game game, IFormFile photo)
        {
            var oldGame = gameService.findById(game.MaGame);
            if (oldGame == null)
            {
                TempData["Msg"] = "❌ Game không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index");
            }

            // Trim và validate
            game.TenGame = game.TenGame?.Trim();
            game.MaTheLoai = game.MaTheLoai?.Trim();

            if (string.IsNullOrWhiteSpace(game.TenGame))
            {
                TempData["Msg"] = "❌ Tên game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }
            if (string.IsNullOrWhiteSpace(game.MaTheLoai))
            {
                TempData["Msg"] = "❌ Thể loại game không được để trống!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }
            if (game.Gia <= 0)
            {
                TempData["Msg"] = "❌ Giá game phải lớn hơn 0!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }

            if (game.NgayRaMat == default)
            {
                game.NgayRaMat = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            string newFileName = null;
            var oldImage = oldGame.Hinh;

            try
            {
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
                        return View("~/Views/Admin/Game/Edit.cshtml", game);
                    }

                    var uploadsFolder = Path.Combine(env.WebRootPath, "images");
                    newFileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadsFolder, newFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }

                    oldGame.Hinh = newFileName;
                }

                gameService.Update(oldGame);

                // Xóa ảnh cũ nếu có ảnh mới
                if (newFileName != null && !string.IsNullOrEmpty(oldImage))
                {
                    var oldPath = Path.Combine(env.WebRootPath, "images", oldImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                TempData["Msg"] = "✅ Chỉnh sửa game thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index");
            }
            catch
            {
                if (newFileName != null)
                {
                    var path = Path.Combine(env.WebRootPath, "images", newFileName);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                TempData["Msg"] = "❌ Chỉnh sửa game thất bại!";
                TempData["MsgType"] = "danger";
                ViewBag.Categories = new SelectList(categoryService.findAll(), "MaTheLoai", "TenLoaiGame", game.MaTheLoai);
                return View("~/Views/Admin/Game/Edit.cshtml", game);
            }
        }

        [Route("game/delete/{id}")]
        public IActionResult Delete(string id, int page = 1)
        {
            var game = gameService.findById(id);
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

            // kiểm tra nếu page hiện tại trống thì giảm page
            int totalItems = gameService.CountGames("", "");
            int maxPage = (int)Math.Ceiling((double)totalItems / pageSize);
            if (page > maxPage) page = maxPage;

            return RedirectToAction("Index", new { page = page });
        }
    }
}