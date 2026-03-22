using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;


namespace GameStore.Services
{
    public class CategoryServiceImpl : CategoryService
    {
        private GameStoreContext db;
        private readonly IDistributedCache cache;

        public CategoryServiceImpl(GameStoreContext _db, IDistributedCache _cache)
        {
            db = _db;
            cache = _cache;
        }

        public bool Create(TheLoaiGame category)
        {
            try
            {
                db.TheLoaiGames.Add(category);
                var result = db.SaveChanges() > 0;

                if (result)
                    cache.Remove("categories");

                return result;
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
                var entity = db.TheLoaiGames.Find(id);
                if (entity == null) return false;

                db.TheLoaiGames.Remove(entity);
                var result = db.SaveChanges() > 0;

                if (result)
                    cache.Remove("categories");

                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool Update(TheLoaiGame category)
        {
            try
            {
                db.Entry(category).State = EntityState.Modified;
                var result = db.SaveChanges() > 0;

                if (result)
                    cache.Remove("categories");

                return result;
            }
            catch
            {
                return false;
            }
        }

        public List<TheLoaiGame> findAll()
        {
            string cacheKey = "categories";

            var cached = cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                Console.WriteLine("🔥 CATEGORY FROM CACHE");
                return JsonSerializer.Deserialize<List<TheLoaiGame>>(cached);
            }

            Console.WriteLine("🐢 CATEGORY FROM DB");

            var data = db.TheLoaiGames.OrderBy(x => x.MaTheLoai).ToList();

            cache.SetString(cacheKey,
                JsonSerializer.Serialize(data),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

            return data;
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

     
    }
}
