using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    public class UserController: Controller
    {

        private UserService userService;

        public UserController(UserService _userService)
        {
            userService = _userService; 
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst("UserId").Value);
        }

        [Route("user/index")]
        public IActionResult Index(string keyword = "", int page = 1)
        {
            int pageSize = 10;

            int totalPages;

            var users = userService.findAll(keyword, page, pageSize, out totalPages);

            var vm = new GameStore.Pagination.Admin.UserListVM
            {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword
            };

            return View("~/Views/Admin/User/Index.cshtml", vm);
        }

        [Route("user/add")]
        public IActionResult Add()
        {
            return View("~/Views/Admin/User/Add.cshtml");
        }

        [HttpPost]
        [Route("user/add")]
        public IActionResult Add(NguoiDung user, string confirmPassword)
        {
            // check confirm password
            if (user.MatKhau != confirmPassword)
            {
                TempData["Msg"] = "Password không khớp";
                return View("~/Views/Admin/User/Add.cshtml", user);
            }
            else if (userService.Create(user))
            {
                TempData["Msg"] = "Add Oke";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Msg"] = "Add Failed";
                return View("~/Views/Admin/User/Add.cshtml", user);
            }
                
        }

        [Route("user/delete/{id}")]
        public IActionResult Delete(int id)
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
        public IActionResult Edit(int id)
        {
            return View("~/Views/Admin/User/Edit.cshtml", userService.findById(id));

        }

        [HttpPost]
        [Route("user/edit/{id}")]
        public IActionResult Edit(NguoiDung user, string confirmPassword)
        {
            // check confirm password
            if (user.MatKhau != confirmPassword)
            {
                TempData["Msg"] = "Password không khớp";
                return View("~/Views/Admin/User/Edit.cshtml", user);
            }
            else if (userService.Update(user))
            {
                TempData["Msg"] = "Edit Oke";
                return RedirectToAction("Index");

            }
            else
            {
                TempData["Msg"] = "Edit Failed";
                return RedirectToAction("Edit", new { id = user.MaNguoiDung });

            }

        }

    

    }
}
