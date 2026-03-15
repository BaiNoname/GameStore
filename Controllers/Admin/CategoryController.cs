using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
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
        public IActionResult Index()
        {
            ViewBag.categories = categoryService.findAll();
            return View("~/Views/Admin/Category/Index.cshtml");
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
