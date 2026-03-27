using GameStore.Helpers;
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

        private readonly MailHelper mailHelper;

        public AuthController(AuthService _authService, MailHelper _mailHelper)
        {
            authService = _authService;
            mailHelper = _mailHelper;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            ViewBag.HideSubBar = true;
            return View();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewBag.HideSubBar = true;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

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
                new Claim(ClaimTypes.Role, user.Quyen.ToLower()),
                new Claim("UserId", user.MaNguoiDung.ToString())
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
            TempData["Success"] = "Đăng nhập thành công!";

            if (user.Quyen.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return Redirect("/admin");

            return Redirect("/");
        }

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            ViewBag.HideSubBar = true;
            return View(new NguoiDung());
        }

        [HttpPost("register")]
        public IActionResult Register(NguoiDung user, string confirmPassword)
        {
            ViewBag.HideSubBar = true;

            if (string.IsNullOrWhiteSpace(user.Email) ||
            string.IsNullOrWhiteSpace(user.TenNguoiDung) ||
            string.IsNullOrWhiteSpace(user.MatKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View(user);
            }

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

            TempData["Register Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
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

        [HttpGet("forgot")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("forgot")]
        public IActionResult ForgotPassword(string email)
        {
            email = email.Trim().ToLower();

            if (authService.SendResetCode(email, out string message))
            {
                HttpContext.Session.SetString("ResetEmail", email);

                return RedirectToAction("VerifyCode");
            }

            ViewBag.Error = message;
            return View();
        }

        [HttpGet("verify")]
        public IActionResult VerifyCode()
        {
            var email = HttpContext.Session.GetString("ResetEmail");

            if (email == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        [HttpPost("verify")]
        public IActionResult VerifyCode(string code)
        {
            var email = HttpContext.Session.GetString("ResetEmail");

            Console.WriteLine("EMAIL SESSION: " + email); // debug

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Phiên làm việc hết hạn, vui lòng nhập lại email";
                return RedirectToAction("ForgotPassword");
            }

            if (authService.VerifyResetCode(email, code, out string message))
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = message;
            return View();
        }

        [HttpGet("reset")]
        public IActionResult ResetPassword()
        {
            var email = HttpContext.Session.GetString("ResetEmail");

            if (email == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        [HttpPost("reset")]
        public IActionResult ResetPassword(string password, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("ResetEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            if (authService.ResetPassword(email, password, confirmPassword, out string message))
            {
                HttpContext.Session.Remove("ResetEmail"); // 🔥 xoá sau khi xong

                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Login");
            }

            ViewBag.Error = message;
            return View();
        }

    }
}
