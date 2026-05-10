using GameStore.Helpers;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GameStore.Services
{
    public class AuthServiceImpl : AuthService
    {
        private readonly GameStoreContext db;
        private readonly MailHelper mailHelper;

        public AuthServiceImpl(GameStoreContext _db, MailHelper _mailHelper)
        {
            db = _db;
            mailHelper = _mailHelper;
        }

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        // Đăng ký tài khoản mới
        public bool Register(NguoiDung user, out string message)
        {
            message = "";

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.MatKhau) ||
                string.IsNullOrWhiteSpace(user.TenNguoiDung))
            {
                message = "Vui lòng nhập đầy đủ thông tin";
                return false;
            }

            // Kiểm tra định dạng email
            if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                message = "Email không hợp lệ";
                return false;
            }

            // Kiểm tra độ dài mật khẩu
            if (user.MatKhau.Length < 5)
            {
                message = "Mật khẩu phải >= 5 ký tự";
                return false;
            }

            // Chuẩn hóa email (xóa khoảng trắng và chuyển về chữ thường)
            user.Email = user.Email.Trim().ToLower();

            // Kiểm tra email đã tồn tại chưa
            if (db.NguoiDungs.Any(x => x.Email == user.Email))
            {
                message = "Email đã tồn tại";
                return false;
            }

            // Hash mật khẩu trước khi lưu vào database
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            user.NgayDangKy = DateOnly.FromDateTime(UtcNow());
            user.Quyen = "user";
            user.SoDu = 0;
            user.GioHang = null;
            user.IsActive = true;

            db.NguoiDungs.Add(user);
            db.SaveChanges();

            message = "Đăng ký thành công 🎉";
            return true;
        }
        
        // Đăng nhập bằng email và mật khẩu
        public NguoiDung? Login(string email, string password)
        {
            // Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            email = email.Trim().ToLower();

            // Tìm user theo email và đảm bảo user đó đang hoạt động
            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email && x.IsActive);

            if (user == null || string.IsNullOrWhiteSpace(user.MatKhau))
                return null;

            // So sánh mật khẩu đã nhập với mật khẩu đã hash trong database
            try
            {
                bool check = BCrypt.Net.BCrypt.Verify(password, user.MatKhau);
                if (!check)
                    return null;
            }
            catch
            {
                return null;
            }

            return user;
        }
        
        // Đổi mật khẩu cho user đang đăng nhập
        public bool ChangePassword(int userId, string oldPass, string newPass, string confirmPass, out string message)
        {
            message = "";

            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

            if (user == null)
            {
                message = "User không tồn tại";
                return false;
            }

            // Kiểm tra mật khẩu cũ
            bool check = BCrypt.Net.BCrypt.Verify(oldPass, user.MatKhau);

            if (!check)
            {
                message = "Mật khẩu cũ không đúng";
                return false;
            }

            if (newPass != confirmPass)
            {
                message = "Xác nhận mật khẩu không khớp";
                return false;
            }

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
        
        // Cập nhật tên hiển thị của user
        public bool UpdateName(int userId, string newName, out string message)
        {
            message = "";

            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

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
        
        // Gửi mã reset password về email
        public bool SendResetCode(string email, out string message)
        {
            message = "";

            if (string.IsNullOrWhiteSpace(email))
            {
                message = "Email không hợp lệ";
                return false;
            }

            email = email.Trim().ToLower();

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email && x.IsActive);

            if (user == null)
            {
                message = "Email không tồn tại";
                return false;
            }

            // Tạo mã reset gồm 6 chữ số ngẫu nhiên
            var code = new Random().Next(100000, 999999).ToString();

            // Lưu mã reset và thời gian hết hạn vào database
            user.ResetCode = code;
            user.ResetCodeExpiry = UtcNow().AddMinutes(5);
            user.IsVerified = false;

            db.SaveChanges();

            var subject = "Reset Password - GameStore";
            var body = $"<h3>Mã reset của bạn là: <b>{code}</b></h3><p>Hết hạn sau 5 phút</p>";

            // Gửi email bất đồng bộ và chờ kết quả
            bool sent = Task.Run(() => mailHelper.SendEmail(email, subject, body)).Result;

            if (!sent)
            {
                message = "Không gửi được email";
                return false;
            }

            message = "Đã gửi mã về email";
            return true;
        }
        
        // Xác thực mã reset password
        public bool VerifyResetCode(string email, string code, out string message)
        {
            message = "";

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email && x.IsActive);

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

            if (user.ResetCodeExpiry == null || user.ResetCodeExpiry < UtcNow())
            {
                message = "Mã đã hết hạn";
                return false;
            }

            user.IsVerified = true;
            db.SaveChanges();

            message = "Xác thực thành công";
            return true;
        }
        
        // Đặt lại mật khẩu sau khi xác thực mã thành công
        public bool ResetPassword(string email, string newPass, string confirmPass, out string message)
        {
            message = "";

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email && x.IsActive);

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
            user.ResetCode = null;
            user.ResetCodeExpiry = null;
            user.IsVerified = false;

            db.SaveChanges();

            message = "Đổi mật khẩu thành công ✅";
            return true;
        }
    }
}