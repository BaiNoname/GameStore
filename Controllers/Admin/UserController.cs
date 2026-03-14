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

    }
}
