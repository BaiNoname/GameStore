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
            throw new NotImplementedException();
        }

        public bool Delete(string id)
        {
            throw new NotImplementedException();
        }

        public List<NguoiDung> findAll()
        {
            return db.NguoiDungs.ToList();
        }

        public bool Update(NguoiDung user)
        {
            throw new NotImplementedException();
        }
    }
}
