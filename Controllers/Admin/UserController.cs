using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Controller quản lý user, chỉ admin mới có quyền truy cập
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class UserController : Controller
    {
        private readonly UserService userService;

        public UserController(UserService _userService)
        {
            userService = _userService;
        }

        // Lấy ID người dùng hiện tại từ claims
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst("UserId")!.Value);
        }

        // Hiển thị danh sách user với phân trang, tìm kiếm và lọc trạng thái
        [Route("user/index")]
        public IActionResult Index(string keyword = "", string status = "all", int page = 1)
        {
            int pageSize = 10;
            int totalPages;

            var users = userService.findAll(keyword, status, page, pageSize, out totalPages);

            var vm = new GameStore.Pagination.Admin.UserListVM
            {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword
            };

            ViewBag.Status = status;

            return View("~/Views/Admin/User/Index.cshtml", vm);
        }

        // Hiển thị form thêm user mới
        [Route("user/add")]
        public IActionResult Add(string keyword = "", string status = "all", int page = 1)
        {
            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            return View("~/Views/Admin/User/Add.cshtml", new NguoiDung { Quyen = "user" });
        }

        // Xử lý logic thêm user mới
        [HttpPost]
        [Route("user/add")]
        public IActionResult Add(NguoiDung user, string confirmPassword, string keyword = "", string status = "all", int page = 1)
        {
            // Chuẩn hóa dữ liệu đầu vào
            user.Email = user.Email?.Trim().ToLower();
            user.TenNguoiDung = user.TenNguoiDung?.Trim();
            user.Quyen = (user.Quyen ?? "").Trim().ToLower();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            // Validate dữ liệu đầu vào
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
            else if (userService.IsEmailExists(user.Email))
            {
                TempData["Msg"] = "Email đã tồn tại";
                TempData["MsgType"] = "danger";
            }
            else if (userService.Create(user))
            {
                TempData["Msg"] = "Add Oke";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, status, page });
            }
            else
            {
                TempData["Msg"] = "Add Failed";
                TempData["MsgType"] = "danger";
            }

            return View("~/Views/Admin/User/Add.cshtml", user);
        }

        // Xử lý logic xoá user (chuyển sang Inactive)
        [Route("user/delete/{id}")]
        public IActionResult Delete(int id, string keyword = "", string status = "all", int page = 1)
        {
            // Kiểm tra xem user cần xoá có tồn tại không
            var userToDelete = userService.findById(id);
            if (userToDelete == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            // Lấy ID người dùng hiện tại để kiểm tra nếu họ đang cố gắng xoá chính mình
            var currentUserId = GetCurrentUserId();

            // Không cho phép xoá chính mình
            if (userToDelete.MaNguoiDung == currentUserId)
            {
                TempData["Msg"] = "❌ Bạn không thể xoá chính mình!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            // Không cho phép xoá admin khác
            if ((userToDelete.Quyen ?? "").Trim().ToLower() == "admin")
            {
                TempData["Msg"] = "❌ Không thể xoá admin khác!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            // Thực hiện xoá (chuyển sang Inactive)
            if (userService.Delete(id, currentUserId))
            {
                TempData["Msg"] = "✅ Chuyển user sang Inactive thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Thao tác thất bại!";
                TempData["MsgType"] = "danger";
            }

            int pageSize = 10;
            int totalPages;
            userService.findAll(keyword, status, 1, pageSize, out totalPages);

            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            return RedirectToAction("Index", new { keyword, status, page });
        }

        // Hiển thị form chỉnh sửa user
        [Route("user/edit/{id}")]
        public IActionResult Edit(int id, string keyword = "", string status = "all", int page = 1)
        {
            // Kiểm tra xem user cần chỉnh sửa có tồn tại không
            var user = userService.findById(id);
            if (user == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            // Lấy ID người dùng hiện tại để kiểm tra nếu họ đang cố gắng chỉnh sửa admin khác
            int currentUserId = GetCurrentUserId();

            // Không cho phép chỉnh sửa admin khác
            if ((user.Quyen ?? "").Trim().ToLower() == "admin" && user.MaNguoiDung != currentUserId)
            {
                TempData["Msg"] = "⚠️ Bạn không thể chỉnh sửa admin khác!";
                TempData["MsgType"] = "warning";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/User/Edit.cshtml", user);
        }

        // Xử lý logic chỉnh sửa user
        [HttpPost]
        [Route("user/edit/{id}")]
        public IActionResult Edit(NguoiDung user, string confirmPassword, string keyword = "", string status = "all", int page = 1)
        {
            // Kiểm tra xem user cần chỉnh sửa có tồn tại không
            var dbUser = userService.findById(user.MaNguoiDung);
            if (dbUser == null)
            {
                TempData["Msg"] = "User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            bool isChanged = false;

            // Tạo một bản sao của user từ database để cập nhật
            var updateUser = new NguoiDung
            {
                MaNguoiDung = dbUser.MaNguoiDung,
                TenNguoiDung = dbUser.TenNguoiDung,
                Email = dbUser.Email,
                Quyen = dbUser.Quyen,
                SoDu = dbUser.SoDu,
                IsActive = dbUser.IsActive,
                MatKhau = null
            };

            // Chuẩn hóa dữ liệu đầu vào
            if (!string.IsNullOrWhiteSpace(user.TenNguoiDung) && user.TenNguoiDung.Trim() != dbUser.TenNguoiDung)
            {
                updateUser.TenNguoiDung = user.TenNguoiDung.Trim();
                isChanged = true;
            }

            //  Chỉ cập nhật email nếu có thay đổi và không trùng với email của user khác
            if (!string.IsNullOrWhiteSpace(user.Email) && user.Email.Trim().ToLower() != dbUser.Email)
            {
                var newEmail = user.Email.Trim().ToLower();

                if (userService.IsEmailExistsForOtherUser(newEmail, dbUser.MaNguoiDung))
                {
                    TempData["Msg"] = "❌ Email đã tồn tại";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/User/Edit.cshtml", user);
                }

                updateUser.Email = newEmail;
                isChanged = true;
            }

            // Cập nhật password nếu có nhập và hợp lệ
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

                updateUser.MatKhau = user.MatKhau;
                isChanged = true;
            }

            // Logic cập nhật role:
            bool isEditingSelf = dbUser.MaNguoiDung == GetCurrentUserId();
            bool isTargetAdmin = (dbUser.Quyen ?? "").Trim().ToLower() == "admin";

            // Nếu đang chỉnh sửa user khác và user đó là admin, không cho phép thay đổi role
            if (!isEditingSelf && isTargetAdmin)
            {
                updateUser.Quyen = dbUser.Quyen;
            }
            else if (isEditingSelf || !isTargetAdmin)
            {
                var newRole = (user.Quyen ?? "").Trim().ToLower();
                if (newRole != dbUser.Quyen && (newRole == "user" || newRole == "admin"))
                {
                    updateUser.Quyen = newRole;
                    isChanged = true;
                }
            }

            // Cập nhật số dư nếu có thay đổi
            if (user.SoDu != dbUser.SoDu)
            {
                updateUser.SoDu = user.SoDu;
                isChanged = true;
            }

            // Cập nhật trạng thái nếu có thay đổi
            if (user.IsActive != dbUser.IsActive)
            {
                updateUser.IsActive = user.IsActive;
                isChanged = true;
            }

            // Nếu không có gì thay đổi, trả về thông báo và không gọi service
            if (!isChanged)
            {
                TempData["Msg"] = "⚠️ Không có gì thay đổi!";
                TempData["MsgType"] = "info";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            // Thực hiện cập nhật
            if (userService.Update(updateUser))
            {
                TempData["Msg"] = "✅ Chỉnh sửa user thành công!";
                TempData["MsgType"] = "success";

                var allUsers = userService.findAll(keyword, status, 1, int.MaxValue, out int _);

                // Sắp xếp lại danh sách user theo thứ tự Active trước, sau đó mới đến ngày đăng ký để tìm vị trí của user vừa chỉnh sửa
                var sorted = allUsers
                    .OrderByDescending(u => u.IsActive)
                    .ThenByDescending(u => u.NgayDangKy)
                    .ToList();

                int index = sorted.FindIndex(u => u.MaNguoiDung == dbUser.MaNguoiDung);
                int pageSize = 10;
                int targetPage = index >= 0 ? (index / pageSize) + 1 : page;

                return RedirectToAction("Index", new { keyword, status, page = targetPage });
            }
            else
            {
                TempData["Msg"] = "❌ Chỉnh sửa user thất bại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/User/Edit.cshtml", user);
            }
        }

        //  Xử lý logic kích hoạt lại user (chuyển từ Inactive sang Active)
        [Route("user/activate/{id}")]
        public IActionResult Activate(int id, string keyword = "", string status = "all", int page = 1)
        {
            // Kiểm tra xem user cần kích hoạt lại có tồn tại không
            var user = userService.findById(id);
            if (user == null)
            {
                TempData["Msg"] = "❌ User không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (userService.Activate(id))
            {
                TempData["Msg"] = "✅ Kích hoạt lại user thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Kích hoạt user thất bại!";
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }
    }
}