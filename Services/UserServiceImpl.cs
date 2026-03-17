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
                // ❗ check null
                if (user == null)
                    return false;

                // ❗ validate email trùng
                if (db.NguoiDungs.Any(u => u.Email == user.Email))
                    return false;

                // ❗ validate role
                if (user.Quyen != "admin" && user.Quyen != "user")
                    user.Quyen = "user"; // default

                // ❗ set mặc định
                user.SoDu = 0;
                user.NgayDangKy = DateOnly.FromDateTime(DateTime.Now);
                user.GioHang = null;

                // ❗ hash password
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

        public bool Delete(int id)
        {
            try
            {
                var user = db.NguoiDungs.Find(id);

                if (user == null)
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

            int totalItems = query.Count();

            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderBy(u => u.MaNguoiDung)
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

                if (existingUser == null)
                    return false;

                existingUser.TenNguoiDung = user.TenNguoiDung;
                existingUser.Email = user.Email;
                existingUser.Quyen = user.Quyen;
                existingUser.SoDu = user.SoDu;

                // nếu admin nhập password mới thì hash
                if (!string.IsNullOrEmpty(user.MatKhau))
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
    }
}
