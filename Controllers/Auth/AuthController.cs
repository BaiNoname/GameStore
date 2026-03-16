using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameStore.Controllers.Auth
{
    [Route("auth")]

    public class AuthController : Controller
    {
        private AuthService authService;

        public AuthController(AuthService _authService)
        {
            authService = _authService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            ViewBag.HideSubBar = true;
            return View();
        }

        // POST: /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewBag.HideSubBar = true;

            var user = authService.Login(email, password);

            if (user == null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenNguoiDung),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Quyen.ToLower())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            if (user.Quyen.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return Redirect("/admin");

            return Redirect("/");
        }

        // GET: /auth/register
        [HttpGet("register")]
        public IActionResult Register()
        {
            ViewBag.HideSubBar = true;
            return View(new NguoiDung());
        }

        // POST: /auth/register
        [HttpPost("register")]
        public IActionResult Register(NguoiDung user, string confirmPassword)
        {
            ViewBag.HideSubBar = true;

            if (user.MatKhau != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không đúng";
                return View("Register", user);
            }

            bool success = authService.Register(user);

            if (!success)
            {
                ViewBag.Error = "Email đã tồn tại";
                return View("Register", user);
            }

            return RedirectToAction("Login");
        }

        // /auth/logout
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }
    }
}
