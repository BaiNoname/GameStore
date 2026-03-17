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

        public List<TheLoaiGame> findAll(string keyword, int page, int pageSize, out int totalPages)
        {
            var query = db.TheLoaiGames.AsQueryable();

            // 🔍 search theo tên thể loại
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(c => c.TenLoaiGame.ToLower().Contains(keyword));
            }

            int totalItems = query.Count();

            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderBy(c => c.MaTheLoai)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
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
