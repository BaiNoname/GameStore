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

        public List<NguoiDung> findAll()
        {
            return db.NguoiDungs.ToList();
        }
    }
}
