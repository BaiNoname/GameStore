using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin")]
    public class CategoryController: Controller
    {

        private CategoryService categoryService;

        public CategoryController(CategoryService _categoryService)
        {
            categoryService = _categoryService;
        }

        [Route("category/index")]
        public IActionResult Index()
        {
            ViewBag.categories = categoryService.findAll();
            return View("~/Views/Admin/Category/Index.cshtml");
        }
    }
}
