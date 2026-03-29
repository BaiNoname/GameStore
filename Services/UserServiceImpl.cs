using GameStore.Models;

namespace GameStore.Services
{
    public class UserServiceImpl : UserService
    {
        private GameStoreContext db;

        public UserServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public bool Create(NguoiDung user)
        {
            try
            {
                if (user == null)
                    return false;

                // trim và chuẩn hóa email, username
                user.Email = user.Email?.Trim().ToLower();
                user.TenNguoiDung = user.TenNguoiDung?.Trim();

                // validate required
                if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.MatKhau) || string.IsNullOrEmpty(user.TenNguoiDung))
                    return false;

                // validate email format
                if (!System.Text.RegularExpressions.Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return false;

                // validate password length
                if (user.MatKhau.Length < 5)
                    return false;

                // check duplicate email
                if (db.NguoiDungs.Any(u => u.Email == user.Email))
                    return false;

                // default role
                if (user.Quyen != "admin" && user.Quyen != "user")
                    user.Quyen = "user";

                user.SoDu = 0;
                user.NgayDangKy = DateOnly.FromDateTime(DateTime.Now);

                user.GioHang = null;

                // hash password
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

                // ❌ Không cho xoá chính mình
                if (user.MaNguoiDung == currentUserId)
                    return false;

                // ❌ Không cho xoá admin khác
                if (user.Quyen == "admin")
                    return false;

                db.NguoiDungs.Remove(user);
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public List<NguoiDung> findAll(string keyword, int page, int pageSize, out int totalPages)
        {
            var query = db.NguoiDungs.AsQueryable();

            // 🔍 filter theo email
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(keyword));
            }

            query = query.OrderByDescending(u => u.NgayDangKy);

            int totalItems = query.Count();

            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public NguoiDung findById(int id)
        {
            return db.NguoiDungs
                     .FirstOrDefault(user => user.MaNguoiDung == id);
        }

        public bool Update(NguoiDung user)
        {
            try
            {
                var existingUser = db.NguoiDungs.Find(user.MaNguoiDung);
                if (existingUser == null) return false;

                existingUser.TenNguoiDung = user.TenNguoiDung;
                existingUser.Email = user.Email;
                existingUser.Quyen = user.Quyen;
                existingUser.SoDu = user.SoDu;

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
    }
}
