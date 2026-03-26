using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GameStore.Services
{
    public class AuthServiceImpl : AuthService
    {
        private GameStoreContext db;

        public AuthServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public bool Register(NguoiDung user)
        {
            // ===== VALIDATION =====
            if (string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.MatKhau) ||
                string.IsNullOrWhiteSpace(user.TenNguoiDung))
                return false;

            // email format
            if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return false;

            // password yếu
            if (user.MatKhau.Length < 5)
                return false;

            // normalize email
            user.Email = user.Email.Trim().ToLower();

            if (db.NguoiDungs.Any(x => x.Email == user.Email))
                return false;


            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            user.NgayDangKy = DateOnly.FromDateTime(DateTime.Now);
            user.Quyen = "User";
            user.SoDu = 0;
            user.GioHang = null;

            db.NguoiDungs.Add(user);
            db.SaveChanges();

            return true;
        }

        public NguoiDung? Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            email = email.Trim().ToLower();

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
                return null;

            bool check = BCrypt.Net.BCrypt.Verify(password, user.MatKhau);

            if (!check)
                return null;

            return user;
        }

        // ================= CHANGE PASSWORD =================
        public bool ChangePassword(int userId, string oldPass, string newPass, string confirmPass, out string message)
        {
            message = "";

            var user = db.NguoiDungs.Find(userId);

            if (user == null)
            {
                message = "User không tồn tại";
                return false;
            }

            // check mật khẩu cũ
            bool check = BCrypt.Net.BCrypt.Verify(oldPass, user.MatKhau);

            if (!check)
            {
                message = "Mật khẩu cũ không đúng";
                return false;
            }

            // confirm
            if (newPass != confirmPass)
            {
                message = "Xác nhận mật khẩu không khớp";
                return false;
            }

            // validate password
            if (newPass.Length < 5)
            {
                message = "Mật khẩu phải >= 5 ký tự";
                return false;
            }

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPass);

            db.SaveChanges();

            message = "Đổi mật khẩu thành công ✅";

            return true;
        }

        // ================= UPDATE NAME =================
        public bool UpdateName(int userId, string newName, out string message)
        {
            message = "";

            var user = db.NguoiDungs.Find(userId);

            if (user == null)
            {
                message = "User không tồn tại";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                message = "Tên không hợp lệ";
                return false;
            }

            user.TenNguoiDung = newName.Trim();

            db.SaveChanges();

            message = "Cập nhật tên thành công ✅";

            return true;
        }
    }
}
