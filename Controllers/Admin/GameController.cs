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
        private GameService gameService;
        private CategoryService categoryService;
        private readonly IWebHostEnvironment env;

        public GameController(GameStoreContext _db, GameService _gameService, CategoryService _categoryService, IWebHostEnvironment _env )
        {
            db = _db;
            gameService = _gameService;
            categoryService = _categoryService;
            env = _env;
        }

        [Route("game/index")]
        public IActionResult Index(string keyword = "", string categoryId = "", int page = 1)
        {
            int pageSize = 10;
            int totalPages;

            var games = gameService.findAll(keyword, categoryId, page, pageSize, out totalPages);

            var vm = new GameStore.ViewModels.GameListVM
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
            ViewBag.Categories = new SelectList(
                categoryService.findAll(),
                "MaTheLoai",
                "TenLoaiGame"
            );

            return View("~/Views/Admin/Game/Add.cshtml");
        }
        [HttpPost]
        [Route("game/add")]
        public async Task<IActionResult> Add(Game game, IFormFile photo)
        {
            string fileName = null;

            try
            {
                if (gameService.findById(game.MaGame) != null)
                {
                    TempData["Msg"] = "Game ID already exists";
                    return RedirectToAction("Add");
                }

                game.SoLuotTai = 0;

                if (photo != null && photo.Length > 0)
                {
                    var ext = Path.GetExtension(photo.FileName).ToLower();

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

                TempData["Msg"] = "Add Oke";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                if (fileName != null)
                {
                    var path = Path.Combine(env.WebRootPath, "images", fileName);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                TempData["Msg"] = "Add Failed";
                return RedirectToAction("Index");
            }
        }


        [Route("game/delete/{id}")]
        public IActionResult Delete(string id)
        {
            var game = gameService.findById(id);

            if (game != null)
            {
                var path = Path.Combine(env.WebRootPath, "images", game.Hinh);

                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                if (gameService.Delete(id))
                {
                    TempData["Msg"] = "Delete Oke";
                }
                else
                {
                    TempData["Msg"] = "Delete Failed";
                }
            }

            return RedirectToAction("Index");
        }

        [Route("game/edit/{id}")]
        public IActionResult Edit(string id)
        {
            var game = gameService.findById(id);

            ViewBag.Categories = new SelectList(
                categoryService.findAll(),
                "MaTheLoai",
                "TenLoaiGame",
                game.MaTheLoai
            );

            return View("~/Views/Admin/Game/Edit.cshtml", game);
        }

        [HttpPost]
        [Route("game/edit/{id}")]
        public async Task<IActionResult> Edit(Game game, IFormFile photo)
        {
            string newFileName = null;

            try
            {
                var oldGame = gameService.findById(game.MaGame);
                var oldImage = oldGame.Hinh;

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
                        TempData["Msg"] = "Invalid image format";
                        return RedirectToAction("Index");
                    }

                    var uploadsFolder = Path.Combine(env.WebRootPath, "images");

                    // lưu ảnh mới
                    newFileName = Guid.NewGuid().ToString() + ext;

                    var filePath = Path.Combine(uploadsFolder, newFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }

                    oldGame.Hinh = newFileName;
                }

                gameService.Update(oldGame);

                if (newFileName != null && !string.IsNullOrEmpty(oldImage))
                {
                    var oldPath = Path.Combine(env.WebRootPath, "images", oldImage);

                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                TempData["Msg"] = "Edit Oke";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // ❗ nếu DB fail → xóa ảnh vừa upload
                if (newFileName != null)
                {
                    var path = Path.Combine(env.WebRootPath, "images", newFileName);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                TempData["Msg"] = "Edit Failed";
                return RedirectToAction("Index");
            }
        }

    }
}
