using GameStore.Helpers;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GameStore.Services
{
    public class AuthServiceImpl : AuthService
    {
        private GameStoreContext db;
        private MailHelper mailHelper;

        public AuthServiceImpl(GameStoreContext _db, MailHelper _mailHelper)
        {
            db = _db;
            mailHelper = _mailHelper;
        }

        public bool Register(NguoiDung user, out string message)
        {
            message = "";

            if (string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.MatKhau) ||
                string.IsNullOrWhiteSpace(user.TenNguoiDung))
            {
                message = "Vui lòng nhập đầy đủ thông tin";
                return false;
            }

            if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                message = "Email không hợp lệ";
                return false;
            }

            if (user.MatKhau.Length < 5)
            {
                message = "Mật khẩu phải >= 5 ký tự";
                return false;
            }

            user.Email = user.Email.Trim().ToLower();

            if (db.NguoiDungs.Any(x => x.Email == user.Email))
            {
                message = "Email đã tồn tại";
                return false;
            }

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            user.NgayDangKy = DateOnly.FromDateTime(DateTime.Now);
            user.Quyen = "User";
            user.SoDu = 0;
            user.GioHang = null;

            db.NguoiDungs.Add(user);
            db.SaveChanges();

            message = "Đăng ký thành công 🎉";
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

        public bool SendResetCode(string email, out string message)
        {
            message = "";

            if (string.IsNullOrWhiteSpace(email))
            {
                message = "Email không hợp lệ";
                return false;
            }

            email = email.Trim().ToLower();

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                message = "Email không tồn tại";
                return false;
            }

            var code = new Random().Next(100000, 999999).ToString();

            user.ResetCode = code;
            user.ResetCodeExpiry = DateTime.UtcNow.AddMinutes(5);
            user.IsVerified = false;

            db.SaveChanges();

            var subject = "Reset Password - GameStore";

            var body = $"<h3>Mã reset của bạn là: <b>{code}</b></h3><p>Hết hạn sau 5 phút</p>";

            bool sent = Task.Run(() => mailHelper.SendEmail(email, subject, body)).Result;

            if (!sent)
            {
                message = "Không gửi được email";
                return false;
            }

            message = "Đã gửi mã về email";
            return true;
        }

        public bool VerifyResetCode(string email, string code, out string message)
        {
            message = "";

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                message = "User không tồn tại";
                return false;
            }

            if (string.IsNullOrEmpty(user.ResetCode))
            {
                message = "Chưa yêu cầu mã";
                return false;
            }

            if (user.ResetCode != code)
            {
                message = "Mã không đúng";
                return false;
            }

            if (user.ResetCodeExpiry == null || user.ResetCodeExpiry < DateTime.UtcNow)
            {
                message = "Mã đã hết hạn";
                return false;
            }

            user.IsVerified = true;
            db.SaveChanges();

            message = "Xác thực thành công";
            return true;
        }

        public bool ResetPassword(string email, string newPass, string confirmPass, out string message)
        {
            message = "";

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                message = "User không tồn tại";
                return false;
            }

            if (!user.IsVerified)
            {
                message = "Chưa xác thực";
                return false;
            }

            if (newPass != confirmPass)
            {
                message = "Mật khẩu không khớp";
                return false;
            }

            if (newPass.Length < 5)
            {
                message = "Mật khẩu quá yếu";
                return false;
            }

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPass);

            // reset lại code
            user.ResetCode = null;
            user.ResetCodeExpiry = null;
            user.IsVerified = false;

            db.SaveChanges();

            message = "Đổi mật khẩu thành công ✅";
            return true;
        }
    }
}
