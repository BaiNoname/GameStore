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
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
                user.NgayDangKy = DateOnly.FromDateTime(DateTime.Now);

                db.NguoiDungs.Add(user);
                return db.SaveChanges() > 0;
            }
            catch
            {
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

        public List<NguoiDung> findAll()
        {
            return db.NguoiDungs.OrderBy(user => user.MaNguoiDung).ToList();
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
