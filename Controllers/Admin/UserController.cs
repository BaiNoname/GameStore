using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin")]
    public class UserController: Controller
    {

        private UserService userService;

        public UserController(UserService _userService)
        {
            userService = _userService; 
        }

        [Route("user/index")]
        public IActionResult Index()
        {
            ViewBag.users = userService.findAll();
            return View("~/Views/Admin/User/Index.cshtml");
        }

        [Route("user/add")]
        public IActionResult Add()
        {
            return View("~/Views/Admin/User/Add.cshtml");
        }

        [HttpPost]
        [Route("user/add")]
        public IActionResult Add(NguoiDung user)
        {
            if (userService.Create(user))
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

        [Route("user/delete/{id}")]
        public IActionResult Delete(string id)
        {
            if (userService.Delete(id))
            {
                TempData["Msg"] = "Delete Oke";
            }
            else
            {
                TempData["Msg"] = "Delete Failed";
            }
            return RedirectToAction("Index");

        }

        [Route("user/edit/{id}")]
        public IActionResult Edit(string id)
        {
            return View("~/Views/Admin/User/Edit.cshtml", userService.findById(id));

        }

        [HttpPost]
        [Route("user/edit/{id}")]
        public IActionResult Edit(NguoiDung user)
        {
            if (userService.Update(user))
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
