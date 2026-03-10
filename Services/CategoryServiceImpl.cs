using GameStore.Models;

namespace GameStore.Services
{
    public class CategoryServiceImpl : CategoryService
    {
        private GameStoreContext db;

        public CategoryServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public List<TheLoaiGame> findAll()
        {
            return db.TheLoaiGames.ToList();
        }
    }
}
