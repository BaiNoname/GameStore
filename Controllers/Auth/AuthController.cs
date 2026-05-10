using GameStore.Helpers;
using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameStore.Controllers.Auth
{
    // Controller xử lý các chức năng liên quan đến xác thực người dùng như đăng nhập, đăng ký, quên mật khẩu, v.v.
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AuthService authService;
        private readonly MailHelper mailHelper;

        public AuthController(AuthService _authService, MailHelper _mailHelper)
        {
            authService = _authService;
            mailHelper = _mailHelper;
        }

        // Hiển thị trang đăng nhập
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.HideSubBar = true;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Xử lý đăng nhập người dùng
        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewBag.HideSubBar = true;
            ViewBag.ReturnUrl = returnUrl;

            // Kiểm tra nếu email hoặc mật khẩu trống
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            // Thực hiện đăng nhập thông qua AuthService
            var user = authService.Login(email, password);

            // Nếu đăng nhập thất bại, hiển thị thông báo lỗi
            if (user == null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu";
                return View();
            }

            // Tạo các claim để lưu thông tin người dùng trong cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenNguoiDung),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Quyen.ToLower()),
                new Claim("UserId", user.MaNguoiDung.ToString())
            };

            // Tạo identity và principal để đăng nhập bằng cookie
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            // Tạo principal từ identity
            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập người dùng bằng cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            TempData["ToastMessage"] = "Đăng nhập thành công!";
            TempData["ToastType"] = "success";

            // Nếu có returnUrl và đó là URL nội bộ, chuyển hướng đến returnUrl
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Nếu người dùng là admin, chuyển hướng đến trang admin
            if (user.Quyen.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return Redirect("/admin");

            return Redirect("/");
        }

        // Hiển thị trang truy cập bị từ chối
        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Hiển thị trang đăng ký
        [HttpGet("register")]
        public IActionResult Register()
        {
            ViewBag.HideSubBar = true;
            return View(new NguoiDung());
        }

        // Xử lý đăng ký người dùng mới
        [HttpPost("register")]
        public IActionResult Register(NguoiDung user, string confirmPassword)
        {
            ViewBag.HideSubBar = true;

            if (user.MatKhau != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không đúng";
                return View(user);
            }

            // Thực hiện đăng ký thông qua AuthService
            bool success = authService.Register(user, out string message);

            if (!success)
            {
                ViewBag.Error = message;
                return View(user);
            }

            TempData["Register Success"] = message;
            return RedirectToAction("Login");
        }

        // Xử lý đăng xuất người dùng
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            // Xóa session và đăng xuất khỏi cookie
            HttpContext.Session.Clear();

            // Đăng xuất người dùng bằng cookie
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "Đã đăng xuất tài khoản";
            TempData["ToastType"] = "success";

            return RedirectToAction("Login");
        }

        // Hiển thị trang quên mật khẩu
        [HttpGet("forgot")]
        public IActionResult ForgotPassword()
        {
            ViewBag.HideSubBar = true;
            return View();
        }

        // Xử lý yêu cầu quên mật khẩu
        [HttpPost("forgot")]
        public IActionResult ForgotPassword(string email)
        {
            ViewBag.HideSubBar = true;
            email = email.Trim().ToLower();

            // Kiểm tra nếu email trống
            if (authService.SendResetCode(email, out string message))
            {
                HttpContext.Session.SetString("ResetEmail", email);
                return RedirectToAction("VerifyCode");
            }

            ViewBag.Error = message;
            return View();
        }

        //  Hiển thị trang xác nhận mã reset mật khẩu
        [HttpGet("verify")]
        public IActionResult VerifyCode()
        {
            ViewBag.HideSubBar = true;
            var email = HttpContext.Session.GetString("ResetEmail");

            // Nếu không có email trong session, chuyển hướng về trang quên mật khẩu
            if (email == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        // Xử lý xác nhận mã reset mật khẩu
        [HttpPost("verify")]
        public IActionResult VerifyCode(string code)
        {
            ViewBag.HideSubBar = true;
            // Lấy email từ session để xác thực mã reset
            var email = HttpContext.Session.GetString("ResetEmail");

            // Nếu không có email trong session, chuyển hướng về trang quên mật khẩu
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Phiên làm việc hết hạn, vui lòng nhập lại email";
                return RedirectToAction("ForgotPassword");
            }

            // Xác thực mã reset thông qua AuthService
            if (authService.VerifyResetCode(email, code, out string message))
            {
                TempData["ToastMessage"] = "Xác nhận mã thành công 🎉";
                TempData["ToastType"] = "success";
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = message;
            return View();
        }

        // Hiển thị trang đặt lại mật khẩu mới
        [HttpGet("reset")]
        public IActionResult ResetPassword()
        {
            ViewBag.HideSubBar = true;
            // Lấy email từ session để đảm bảo người dùng đã xác thực mã reset trước khi cho phép đặt lại mật khẩu
            var email = HttpContext.Session.GetString("ResetEmail");

            if (email == null)
                return RedirectToAction("ForgotPassword");

            return View();
        }

        // Xử lý đặt lại mật khẩu mới
        [HttpPost("reset")]
        public IActionResult ResetPassword(string password, string confirmPassword)
        {
            ViewBag.HideSubBar = true;
            // Lấy email từ session để đảm bảo người dùng đã xác thực mã reset trước khi cho phép đặt lại mật khẩu
            var email = HttpContext.Session.GetString("ResetEmail");

            // Nếu không có email trong session, chuyển hướng về trang quên mật khẩu
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Kiểm tra nếu mật khẩu mới và mật khẩu xác nhận không khớp
            if (authService.ResetPassword(email, password, confirmPassword, out string message))
            {
                HttpContext.Session.Remove("ResetEmail");
                TempData["Reset Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Login");
            }

            ViewBag.Error = message;
            return View();
        }
    }
}