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
            // trim input
            user.Email = user.Email?.Trim().ToLower();
            user.TenNguoiDung = user.TenNguoiDung?.Trim();

            // validate required
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
                return RedirectToAction("Index", new { page = 1 });
            }
            else
            {
                TempData["Msg"] = "Add Failed";
                TempData["MsgType"] = "danger";
            }

            return View("~/Views/Admin/User/Add.cshtml", user);
        }

        [Route("user/delete/{id}")]
        public IActionResult Delete(int id, int page = 1)
        {
            var userToDelete = userService.findById(id);
            if (userToDelete == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { page = page });
            }

            var currentUserId = GetCurrentUserId();

            // ❌ Không cho xoá chính mình
            if (userToDelete.MaNguoiDung == currentUserId)
            {
                TempData["Msg"] = "❌ Bạn không thể xoá chính mình!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { page = page });
            }

            // ❌ Không cho xoá các admin khác
            if (userToDelete.Quyen == "admin")
            {
                TempData["Msg"] = "❌ Không thể xoá admin khác!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { page = page });
            }

            // Xoá bình thường
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

            return RedirectToAction("Index", new { page = page });
        }

        [Route("user/edit/{id}")]
        public IActionResult Edit(int id)
        {
            var user = userService.findById(id);
            if (user == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index");
            }

            int currentUserId = GetCurrentUserId();

            // ❌ Không cho edit admin khác
            if (user.Quyen == "admin" && user.MaNguoiDung != currentUserId)
            {
                TempData["Msg"] = "⚠️ Bạn không thể chỉnh sửa admin khác!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index");
            }

            return View("~/Views/Admin/User/Edit.cshtml", user);
        }

        [HttpPost]
        [Route("user/edit/{id}")]
        public IActionResult Edit(NguoiDung user, string confirmPassword)
        {
            var existingUser = userService.findById(user.MaNguoiDung);
            if (existingUser == null)
            {
                TempData["Msg"] = "User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index");
            }

            bool isChanged = false;

            // Update tên
            if (!string.IsNullOrWhiteSpace(user.TenNguoiDung) && user.TenNguoiDung.Trim() != existingUser.TenNguoiDung)
            {
                existingUser.TenNguoiDung = user.TenNguoiDung.Trim();
                isChanged = true;
            }

            // Update email
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

            // Update password
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

            // Update role
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

            // Update balance
            if (user.SoDu != existingUser.SoDu)
            {
                existingUser.SoDu = user.SoDu;
                isChanged = true;
            }

            if (!isChanged)
            {
                TempData["Msg"] = "⚠️ Không có gì thay đổi!";
                TempData["MsgType"] = "info";
                return RedirectToAction("Index");
            }

            // Cập nhật DB
            if (userService.Update(existingUser))
            {
                TempData["Msg"] = "✅ Chỉnh sửa user thành công!";
                TempData["MsgType"] = "success";

                // Tính trang chứa user
                var allUsers = userService.findAll("", 1, int.MaxValue, out int _);
                var sorted = allUsers.OrderByDescending(u => u.NgayDangKy).ToList();
                int index = sorted.FindIndex(u => u.MaNguoiDung == existingUser.MaNguoiDung);
                int pageSize = 10;
                int page = (index / pageSize) + 1;

                return RedirectToAction("Index", new { page = page });
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
