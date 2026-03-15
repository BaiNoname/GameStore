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

        public bool Create(TheLoaiGame category)
        {
            try
            {
                db.TheLoaiGames.Add(category);
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
                db.TheLoaiGames.Remove(db.TheLoaiGames.Find(id));
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public List<TheLoaiGame> findAll()
        {
            return db.TheLoaiGames.OrderBy(tl => tl.MaTheLoai).ToList();
        }

        public TheLoaiGame findById(string id)
        {
            return db.TheLoaiGames
                     .FirstOrDefault(c => c.MaTheLoai == id);
        }

        public bool Update(TheLoaiGame category)
        {
            try
            {
                db.Entry(category).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
