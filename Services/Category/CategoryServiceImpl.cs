using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Diagnostics;


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

        // Thêm danh mục game mới
        public bool Create(TheLoaiGame category)
        {
            try
            {
                db.TheLoaiGames.Add(category);
                var result = db.SaveChanges() > 0;

                if (result)
                {
                    cache.Remove("categories");
                    Console.WriteLine("🗑️ REDIS REMOVE: categories (CREATE)");
                }

                return result;
            }
            catch
            {
                return false;
            }
        }
        
        // Xóa danh mục game
        public bool Delete(string id)
        {
            try
            {
                var entity = db.TheLoaiGames.Find(id);
                if (entity == null) return false;

                db.TheLoaiGames.Remove(entity);
                var result = db.SaveChanges() > 0;

                if (result)
                {
                    cache.Remove("categories");
                    Console.WriteLine("🗑️ REDIS REMOVE: categories (DELETE)");
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        // Cập nhật danh mục game
        public bool Update(TheLoaiGame category)
        {
            try
            {
                db.Entry(category).State = EntityState.Modified;
                var result = db.SaveChanges() > 0;

                if (result)
                {
                    cache.Remove("categories");
                    Console.WriteLine("🗑️ REDIS REMOVE: categories (UPDATE)");
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        // Lấy tất cả danh mục game (có sử dụng cache Redis)
        public List<TheLoaiGame> findAll()
        {
            
            string cacheKey = "categories";

            var totalWatch = Stopwatch.StartNew();

            Console.WriteLine("🔍 CHECK CACHE: categories");

            // ⏱ Đo thời gian truy xuất cache
            var cacheWatch = Stopwatch.StartNew();
            var cached = cache.GetString(cacheKey);
            cacheWatch.Stop();

            Console.WriteLine($"⏱ REDIS TIME: {cacheWatch.ElapsedMilliseconds} ms");

            // Nếu có cache, trả về dữ liệu từ cache
            if (!string.IsNullOrEmpty(cached))
            {
                Console.WriteLine("🔥 REDIS HIT: categories");
                Console.WriteLine($"📦 SIZE: {cached.Length} chars");

                totalWatch.Stop();
                Console.WriteLine($"⚡ TOTAL TIME (CACHE): {totalWatch.ElapsedMilliseconds} ms");

                // ⏱ Đo thời gian deserialize
                return JsonSerializer.Deserialize<List<TheLoaiGame>>(cached);
            }

            Console.WriteLine("🐢 REDIS MISS → QUERY DB");

            var dbWatch = Stopwatch.StartNew();

            var data = db.TheLoaiGames
                .OrderBy(x => x.MaTheLoai)
                .ToList();

            dbWatch.Stop();

            Console.WriteLine($"⏱ DB TIME: {dbWatch.ElapsedMilliseconds} ms");

            var serializeWatch = Stopwatch.StartNew();

            // Chuyển dữ liệu thành JSON để lưu vào cache
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });

            // Lưu vào cache với thời gian hết hạn 10 phút
            cache.SetString(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            serializeWatch.Stop();

            Console.WriteLine($"⏱ SERIALIZE + SET CACHE: {serializeWatch.ElapsedMilliseconds} ms");

            totalWatch.Stop();
            Console.WriteLine($"⚡ TOTAL TIME (DB): {totalWatch.ElapsedMilliseconds} ms");

            return data;
        }

        // Lấy danh mục game theo từ khóa tìm kiếm (có phân trang)
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
        
        // Lấy danh mục game theo ID
        public TheLoaiGame findById(string id)
        {
            return db.TheLoaiGames
                     .FirstOrDefault(c => c.MaTheLoai == id);
        }

     
    }
}
