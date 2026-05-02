using GameStore.Models;

namespace GameStore.Services
{
    public class UserServiceImpl : UserService
    {
        private readonly GameStoreContext db;

        public UserServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        public bool Create(NguoiDung user)
        {
            try
            {
                if (user == null)
                    return false;

                user.Email = user.Email?.Trim().ToLower();
                user.TenNguoiDung = user.TenNguoiDung?.Trim();
                user.Quyen = (user.Quyen ?? "").Trim().ToLower();

                if (string.IsNullOrEmpty(user.Email) ||
                    string.IsNullOrEmpty(user.MatKhau) ||
                    string.IsNullOrEmpty(user.TenNguoiDung))
                    return false;

                if (!System.Text.RegularExpressions.Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return false;

                if (user.MatKhau.Length < 5)
                    return false;

                if (db.NguoiDungs.Any(u => u.Email == user.Email))
                    return false;

                if (user.Quyen != "admin" && user.Quyen != "user")
                    user.Quyen = "user";

                user.SoDu = 0;
                user.NgayDangKy = DateOnly.FromDateTime(UtcNow());
                user.GioHang = null;
                user.IsActive = true;

                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);

                db.NguoiDungs.Add(user);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR CREATE USER: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int id, int currentUserId)
        {
            try
            {
                var user = db.NguoiDungs.Find(id);
                if (user == null)
                    return false;

                if (user.MaNguoiDung == currentUserId)
                    return false;

                if ((user.Quyen ?? "").Trim().ToLower() == "admin")
                    return false;

                if (!user.IsActive)
                    return false;

                user.IsActive = false;
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool Activate(int id)
        {
            try
            {
                var user = db.NguoiDungs.Find(id);
                if (user == null)
                    return false;

                if (user.IsActive)
                    return true;

                user.IsActive = true;
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public List<NguoiDung> findAll(string keyword, string status, int page, int pageSize, out int totalPages)
        {
            var query = db.NguoiDungs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(keyword) ||
                    u.TenNguoiDung.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim().ToLower();

                if (normalized == "active")
                    query = query.Where(u => u.IsActive);
                else if (normalized == "inactive")
                    query = query.Where(u => !u.IsActive);
            }

            query = query
                .OrderByDescending(u => u.IsActive)
                .ThenByDescending(u => u.NgayDangKy);

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public NguoiDung findById(int id)
        {
            return db.NguoiDungs.FirstOrDefault(user => user.MaNguoiDung == id);
        }

        public bool Update(NguoiDung user)
        {
            try
            {
                var existingUser = db.NguoiDungs.Find(user.MaNguoiDung);
                if (existingUser == null)
                    return false;

                var email = user.Email?.Trim().ToLower();
                var name = user.TenNguoiDung?.Trim();
                var role = (user.Quyen ?? "").Trim().ToLower();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
                    return false;

                bool duplicateEmail = db.NguoiDungs.Any(u =>
                    u.MaNguoiDung != user.MaNguoiDung &&
                    u.Email.ToLower() == email);

                if (duplicateEmail)
                    return false;

                if (role != "admin" && role != "user")
                    role = "user";

                existingUser.TenNguoiDung = name;
                existingUser.Email = email;
                existingUser.Quyen = role;
                existingUser.SoDu = user.SoDu;
                existingUser.IsActive = user.IsActive;

                if (!string.IsNullOrWhiteSpace(user.MatKhau))
                {
                    existingUser.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
                }

                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool IsEmailExists(string email)
        {
            email = email?.Trim().ToLower();
            return db.NguoiDungs.Any(u => u.Email.ToLower() == email);
        }

        public bool IsEmailExistsForOtherUser(string email, int userId)
        {
            email = email?.Trim().ToLower();
            return db.NguoiDungs.Any(u =>
                u.MaNguoiDung != userId &&
                u.Email.ToLower() == email);
        }
    }
}