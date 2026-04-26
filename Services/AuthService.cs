using GameStore.Models;

namespace GameStore.Services
{
    public interface AuthService
    {
        bool Register(NguoiDung user, out string message);
        NguoiDung? Login(string email, string password);
        bool ChangePassword(int userId, string oldPass, string newPass, string confirmPass, out string message);
        bool UpdateName(int userId, string newName, out string message);
        bool SendResetCode(string email, out string message);
        bool VerifyResetCode(string email, string code, out string message);
        bool ResetPassword(string email, string newPass, string confirmPass, out string message);
    }
}