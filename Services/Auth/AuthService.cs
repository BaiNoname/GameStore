using GameStore.Models;

namespace GameStore.Services
{
    // Service xử lý các chức năng xác thực tài khoản
    public interface AuthService
    {
        // Đăng ký tài khoản mới
        bool Register(NguoiDung user, out string message);

        // Đăng nhập bằng email và mật khẩu
        NguoiDung? Login(string email, string password);

        // Đổi mật khẩu cho user đang đăng nhập
        bool ChangePassword(int userId, string oldPass, string newPass, string confirmPass, out string message);

        // Cập nhật tên hiển thị của user
        bool UpdateName(int userId, string newName, out string message);

        // Gửi mã reset password về email
        bool SendResetCode(string email, out string message);

        // Xác thực mã reset password
        bool VerifyResetCode(string email, string code, out string message);

        // Đặt lại mật khẩu sau khi xác thực mã thành công
        bool ResetPassword(string email, string newPass, string confirmPass, out string message);
    }
}