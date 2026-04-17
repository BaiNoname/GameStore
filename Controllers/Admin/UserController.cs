using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class UserController : Controller
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
        public IActionResult Add(string keyword = "", int page = 1)
        {
            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            return View("~/Views/Admin/User/Add.cshtml");
        }

        [HttpPost]
        [Route("user/add")]
        public IActionResult Add(NguoiDung user, string confirmPassword, string keyword = "", int page = 1)
        {
            user.Email = user.Email?.Trim().ToLower();
            user.TenNguoiDung = user.TenNguoiDung?.Trim();

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;

            if (string.IsNullOrWhiteSpace(user.TenNguoiDung))
            {
                TempData["Msg"] = "Tên user không được để trống";
                TempData["MsgType"] = "danger";
            }
            else if (string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["Msg"] = "Email không được để trống";
                TempData["MsgType"] = "danger";
            }
            else if (string.IsNullOrWhiteSpace(user.MatKhau))
            {
                TempData["Msg"] = "Password không được để trống";
                TempData["MsgType"] = "danger";
            }
            else if (user.MatKhau.Length < 5)
            {
                TempData["Msg"] = "Password phải >= 5 ký tự";
                TempData["MsgType"] = "danger";
            }
            else if (user.MatKhau != confirmPassword)
            {
                TempData["Msg"] = "Password không khớp";
                TempData["MsgType"] = "danger";
            }
            else if (userService.findAll(user.Email, 1, 1, out int tmp).Any())
            {
                TempData["Msg"] = "Email đã tồn tại";
                TempData["MsgType"] = "danger";
            }
            else if (userService.Create(user))
            {
                TempData["Msg"] = "Add Oke";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, page });
            }
            else
            {
                TempData["Msg"] = "Add Failed";
                TempData["MsgType"] = "danger";
            }

            return View("~/Views/Admin/User/Add.cshtml", user);
        }

        [Route("user/delete/{id}")]
        public IActionResult Delete(int id, string keyword = "", int page = 1)
        {
            var userToDelete = userService.findById(id);
            if (userToDelete == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, page });
            }

            var currentUserId = GetCurrentUserId();

            if (userToDelete.MaNguoiDung == currentUserId)
            {
                TempData["Msg"] = "❌ Bạn không thể xoá chính mình!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { keyword, page });
            }

            if (userToDelete.Quyen == "admin")
            {
                TempData["Msg"] = "❌ Không thể xoá admin khác!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { keyword, page });
            }

            if (userService.Delete(id, currentUserId))
            {
                TempData["Msg"] = "✅ Delete thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Delete thất bại!";
                TempData["MsgType"] = "danger";
            }

            int pageSize = 10;
            int totalPages;
            userService.findAll(keyword, 1, pageSize, out totalPages);

            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            return RedirectToAction("Index", new { keyword, page });
        }

        [Route("user/edit/{id}")]
        public IActionResult Edit(int id, string keyword = "", int page = 1)
        {
            var user = userService.findById(id);
            if (user == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, page });
            }

            int currentUserId = GetCurrentUserId();

            if (user.Quyen == "admin" && user.MaNguoiDung != currentUserId)
            {
                TempData["Msg"] = "⚠️ Bạn không thể chỉnh sửa admin khác!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { keyword, page });
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/User/Edit.cshtml", user);
        }

        [HttpPost]
        [Route("user/edit/{id}")]
        public IActionResult Edit(NguoiDung user, string confirmPassword, string keyword = "", int page = 1)
        {
            var existingUser = userService.findById(user.MaNguoiDung);
            if (existingUser == null)
            {
                TempData["Msg"] = "User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, page });
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;

            bool isChanged = false;

            if (!string.IsNullOrWhiteSpace(user.TenNguoiDung) && user.TenNguoiDung.Trim() != existingUser.TenNguoiDung)
            {
                existingUser.TenNguoiDung = user.TenNguoiDung.Trim();
                isChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(user.Email) && user.Email.Trim().ToLower() != existingUser.Email)
            {
                var newEmail = user.Email.Trim().ToLower();
                var allUsers = userService.findAll("", 1, int.MaxValue, out int _);
                if (allUsers.Any(u => u.Email == newEmail && u.MaNguoiDung != existingUser.MaNguoiDung))
                {
                    TempData["Msg"] = "❌ Email đã tồn tại";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/User/Edit.cshtml", user);
                }
                existingUser.Email = newEmail;
                isChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(user.MatKhau))
            {
                if (user.MatKhau.Length < 5)
                {
                    TempData["Msg"] = "❌ Password phải >= 5 ký tự";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/User/Edit.cshtml", user);
                }
                if (user.MatKhau != confirmPassword)
                {
                    TempData["Msg"] = "❌ Password không khớp";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/User/Edit.cshtml", user);
                }

                existingUser.MatKhau = user.MatKhau;
                isChanged = true;
            }

            bool isEditingSelf = existingUser.MaNguoiDung == GetCurrentUserId();
            bool isTargetAdmin = existingUser.Quyen == "admin";

            if (!isEditingSelf && isTargetAdmin)
            {
                user.Quyen = existingUser.Quyen;
            }
            else if (isEditingSelf || (!isTargetAdmin))
            {
                if (user.Quyen != existingUser.Quyen && (user.Quyen == "user" || user.Quyen == "admin"))
                {
                    existingUser.Quyen = user.Quyen;
                    isChanged = true;
                }
            }

            if (user.SoDu != existingUser.SoDu)
            {
                existingUser.SoDu = user.SoDu;
                isChanged = true;
            }

            if (!isChanged)
            {
                TempData["Msg"] = "⚠️ Không có gì thay đổi!";
                TempData["MsgType"] = "info";
                return RedirectToAction("Index", new { keyword, page });
            }

            if (userService.Update(existingUser))
            {
                TempData["Msg"] = "✅ Chỉnh sửa user thành công!";
                TempData["MsgType"] = "success";

                var allUsers = userService.findAll(keyword, 1, int.MaxValue, out int _);
                var sorted = allUsers.OrderByDescending(u => u.NgayDangKy).ToList();
                int index = sorted.FindIndex(u => u.MaNguoiDung == existingUser.MaNguoiDung);
                int pageSize = 10;
                int targetPage = index >= 0 ? (index / pageSize) + 1 : page;

                return RedirectToAction("Index", new { keyword, page = targetPage });
            }
            else
            {
                TempData["Msg"] = "❌ Chỉnh sửa user thất bại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/User/Edit.cshtml", user);
            }
        }
    }
}