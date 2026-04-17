using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class CategoryController : Controller
    {
        private CategoryService categoryService;
        private const int pageSize = 10;

        public CategoryController(CategoryService _categoryService)
        {
            categoryService = _categoryService;
        }

        [Route("category")]
        [Route("category/index")]
        public IActionResult Index(string keyword = "", int page = 1)
        {
            int totalPages;

            var categories = categoryService.findAll(keyword, page, pageSize, out totalPages);

            var vm = new GameStore.Pagination.Admin.CategoryListVM
            {
                Categories = categories,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword
            };

            return View("~/Views/Admin/Category/Index.cshtml", vm);
        }

        [Route("category/add")]
        public IActionResult Add(string keyword = "", int page = 1)
        {
            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            return View("~/Views/Admin/Category/Add.cshtml");
        }

        [HttpPost]
        [Route("category/add")]
        public IActionResult Add(TheLoaiGame category, string keyword = "", int page = 1)
        {
            category.MaTheLoai = category.MaTheLoai?.Trim();
            category.TenLoaiGame = category.TenLoaiGame?.Trim();

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;

            if (string.IsNullOrWhiteSpace(category.MaTheLoai))
            {
                TempData["Msg"] = "❌ Mã thể loại không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Category/Add.cshtml", category);
            }

            if (string.IsNullOrWhiteSpace(category.TenLoaiGame))
            {
                TempData["Msg"] = "❌ Tên thể loại không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Category/Add.cshtml", category);
            }

            if (categoryService.findById(category.MaTheLoai) != null)
            {
                TempData["Msg"] = "❌ Mã thể loại đã tồn tại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Category/Add.cshtml", category);
            }

            if (categoryService.Create(category))
            {
                TempData["Msg"] = "✅ Thêm thể loại thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, page });
            }
            else
            {
                TempData["Msg"] = "❌ Thêm thể loại thất bại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Category/Add.cshtml", category);
            }
        }

        [Route("category/delete/{id}")]
        public IActionResult Delete(string id, string keyword = "", int page = 1)
        {
            if (categoryService.Delete(id))
            {
                TempData["Msg"] = "✅ Xóa thể loại thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Xóa thất bại!";
                TempData["MsgType"] = "danger";
            }

            int totalPages;
            categoryService.findAll(keyword, 1, pageSize, out totalPages);

            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            return RedirectToAction("Index", new { keyword, page });
        }

        [Route("category/edit/{id}")]
        public IActionResult Edit(string id, string keyword = "", int page = 1)
        {
            var category = categoryService.findById(id);
            if (category == null)
            {
                TempData["Msg"] = "❌ Category không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, page });
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/Category/Edit.cshtml", category);
        }

        [HttpPost]
        [Route("category/edit/{id}")]
        public IActionResult Edit(TheLoaiGame category, string keyword = "", int page = 1)
        {
            var existing = categoryService.findById(category.MaTheLoai);
            if (existing == null)
            {
                TempData["Msg"] = "❌ Thể loại không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, page });
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;

            bool isChanged = false;

            if (!string.IsNullOrWhiteSpace(category.TenLoaiGame) &&
                category.TenLoaiGame.Trim() != existing.TenLoaiGame)
            {
                existing.TenLoaiGame = category.TenLoaiGame.Trim();
                isChanged = true;
            }

            if (!isChanged)
            {
                TempData["Msg"] = "⚠️ Không có gì thay đổi!";
                TempData["MsgType"] = "info";
                return RedirectToAction("Index", new { keyword, page });
            }

            if (categoryService.Update(existing))
            {
                int targetPage = page;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    var allCategories = categoryService.findAll("", 1, int.MaxValue, out int _)
                                                      .OrderBy(c => c.MaTheLoai)
                                                      .ToList();
                    int index = allCategories.FindIndex(c => c.MaTheLoai == category.MaTheLoai);
                    if (index >= 0)
                    {
                        targetPage = (index / pageSize) + 1;
                    }
                }

                TempData["Msg"] = "✅ Chỉnh sửa thể loại thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, page = targetPage });
            }
            else
            {
                TempData["Msg"] = "❌ Chỉnh sửa thất bại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Category/Edit.cshtml", category);
            }
        }
    }
}