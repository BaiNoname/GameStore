using GameStore.Models;

namespace GameStore.Services
{
    public interface AuthService
    {
        bool Register(NguoiDung user);
        NguoiDung? Login(string email, string password);
    }
}
