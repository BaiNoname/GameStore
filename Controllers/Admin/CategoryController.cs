using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    public class CategoryController : Controller
    {

        private CategoryService categoryService;

        public CategoryController(CategoryService _categoryService)
        {
            categoryService = _categoryService;
        }

        [Route("category")]
        [Route("category/index")]
        public IActionResult Index(string keyword = "", int page = 1)
        {
            int pageSize = 10;
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
        public IActionResult Add()
        {
            return View("~/Views/Admin/Category/Add.cshtml");
        }

        [HttpPost]
        [Route("category/add")]
        public IActionResult Add(TheLoaiGame category)
        {
            if (categoryService.Create(category))
            {
                TempData["Msg"] = "Add Oke";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Msg"] = "Add Failed";
                return RedirectToAction("Add");
            }
        }

        [Route("category/delete/{id}")]
        public IActionResult Delete(string id)
        {
            if (categoryService.Delete(id))
            {
                TempData["Msg"] = "Delete Oke";
            }
            else
            {
                TempData["Msg"] = "Delete Failed";
            }
            return RedirectToAction("Index");

        }

        [Route("category/edit/{id}")]
        public IActionResult Edit(string id)
        {
            return View("~/Views/Admin/Category/Edit.cshtml", categoryService.findById(id));

        }

        [HttpPost]
        [Route("category/edit/{id}")]
        public IActionResult Edit(TheLoaiGame category)
        {
            if (categoryService.Update(category))
            {
                TempData["Msg"] = "Edit Oke";
                return RedirectToAction("Index");

            }
            else
            {
                TempData["Msg"] = "Edit Failed";
                return RedirectToAction("Edit");

            }

        }

    }
}
