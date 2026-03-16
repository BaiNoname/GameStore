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
                db.NguoiDungs.Add(user);
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool Delete(string id)
        {
            try
            {
                db.NguoiDungs.Remove(db.NguoiDungs.Find(id));
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

        public NguoiDung findById(string id)
        {
            return db.NguoiDungs
                     .FirstOrDefault(user => user.MaNguoiDung == id);
        }

        public bool Update(NguoiDung user)
        {
            try
            {
                db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
