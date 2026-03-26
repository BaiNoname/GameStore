using GameStore.Models;

namespace GameStore.Services
{
    public interface AuthService
    {
        bool Register(NguoiDung user);
        NguoiDung? Login(string email, string password);
        bool ChangePassword(int userId, string oldPass, string newPass, string confirmPass, out string message);
        bool UpdateName(int userId, string newName, out string message);
    }
}
