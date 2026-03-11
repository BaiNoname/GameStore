using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Auth
{
    [Route("auth")]

    public class AuthController: Controller
    {
        [Route("login")]
        public IActionResult Login()
        {
            ViewBag.HideSubBar = true;
            return View();
        }

        [Route("register")]
        public IActionResult Register()
        {
            ViewBag.HideSubBar = true;
            return View();
        }
    }
}
