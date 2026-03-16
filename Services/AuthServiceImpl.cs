using GameStore.Models;
using Microsoft.EntityFrameworkCore;

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
            if (db.NguoiDungs.Any(x => x.Email == user.Email))
                return false;

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            user.NgayDangKy = DateOnly.FromDateTime(DateTime.Now);
            user.Quyen = "User";
            user.SoDu = 0;

            db.NguoiDungs.Add(user);
            db.SaveChanges();

            return true;
        }

        public NguoiDung? Login(string email, string password)
        {
            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
                return null;

            bool check = BCrypt.Net.BCrypt.Verify(password, user.MatKhau);

            if (!check)
                return null;

            return user;
        }
    }
}
