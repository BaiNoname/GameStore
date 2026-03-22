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
    }
}
