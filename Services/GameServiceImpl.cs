using GameStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameStore.Services
{
    public class GameServiceImpl : GameService
    {
        private GameStoreContext db;
        private readonly IDistributedCache cache;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };

        public GameServiceImpl(GameStoreContext _db, IDistributedCache _cache)
        {
            db = _db;
            cache = _cache;
        }

        public List<string> GetAllGameNames()
        {
            return db.Games
                .Select(x => x.TenGame)
                .Take(50)
                .ToList();
        }

        public List<Game> findAll()
        {
            
            string cacheKey = "games_all";

            var cachedData = cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Game>>(cachedData, _jsonOptions);
            }

            var games = db.Games.OrderBy(g => g.MaGame).ToList();

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            cache.SetString(
            cacheKey,
            JsonSerializer.Serialize(games, _jsonOptions),
            options
            );
            return games;

        }

        public List<Game> findAll(string keyword, string categoryId, int page, int pageSize, out int totalPages)
        {
            var query = db.Games.Include(g => g.TheLoaiGame).AsQueryable();

            // 🔍 search tên game (không phân biệt hoa thường)
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(g => g.TenGame.ToLower().Contains(keyword));
            }

            // 🎯 filter theo thể loại
            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(g => g.MaTheLoai == categoryId);
            }

            int totalItems = query.Count();

            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderBy(g => g.MaGame)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public Game? findById(string maGame)
        {
            string cacheKey = $"game_{maGame}";

            var cachedData = cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<Game>(cachedData, _jsonOptions);
            }

            var game = db.Games.FirstOrDefault(x => x.MaGame == maGame);

            if (game != null)
            {
                cache.SetString(
                    cacheKey,
                    System.Text.Json.JsonSerializer.Serialize(game),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });
            }

            return game;
        }

        // search theo tên game
        public List<Game> SearchGames(string keyword)
        {
            return db.Games
                     .Where(g => g.TenGame.Contains(keyword))
                     .ToList();
        }

        // lọc theo thể loại
        public List<Game> FilterGames(string search, string category, int page, int pageSize)
        {

            string cacheKey = $"games_{search ?? "all"}_{category ?? "all"}_{page}_{pageSize}";

            var start = DateTime.Now;   
            var cachedData = cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var end = DateTime.Now;
                Console.WriteLine("🔥 CACHE HIT");
                Console.WriteLine($"⏱ CACHE TIME: {(end - start).TotalMilliseconds} ms");
                return System.Text.Json.JsonSerializer.Deserialize<List<Game>>(cachedData);
            }

            Console.WriteLine("🐢 DB QUERY START");

            var dbStart = DateTime.Now;

            var query = db.Games.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(g => g.TenGame.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.MaTheLoai == category);

            var games = query
                .OrderBy(g => g.MaGame)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var dbEnd = DateTime.Now;
            Console.WriteLine("🐢 DB QUERY DONE");
            Console.WriteLine($"⏱ DB TIME: {(dbEnd - dbStart).TotalMilliseconds} ms");

            cache.SetString(
                cacheKey,
                System.Text.Json.JsonSerializer.Serialize(games, _jsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            var end2 = DateTime.Now;
            Console.WriteLine($"⏱ TOTAL TIME: {(end2 - start).TotalMilliseconds} ms");
            return games;
        }

        // Game mới trong 1 tháng
        public List<Game> GetNewGames()
        {
            string key = "games_new";

            var start = DateTime.Now;
            Console.WriteLine($"⏱ GetNewGames START: {start:HH:mm:ss.fff}");

            var cached = cache.GetString(key);

            if (!string.IsNullOrEmpty(cached))
            {
                Console.WriteLine("🔥 NEW GAMES CACHE HIT");
                Console.WriteLine($"⏱ CACHE TIME: {(DateTime.Now - start).TotalMilliseconds} ms");

                return JsonSerializer.Deserialize<List<Game>>(cached);
            }

            Console.WriteLine("🐢 NEW GAMES DB");

            var dbStart = DateTime.Now;

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            var data = db.Games
                .Where(g => g.NgayRaMat >= DateOnly.FromDateTime(oneMonthAgo))
                .OrderByDescending(g => g.NgayRaMat)
                .ToList();

            var dbEnd = DateTime.Now;

            Console.WriteLine("🐢 NEW GAMES DONE");
            Console.WriteLine($"⏱ DB TIME: {(dbEnd - dbStart).TotalMilliseconds} ms");

            cache.SetString(key, JsonSerializer.Serialize(data, _jsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            return data;
        }

        // Game hot (lượt tải cao)
        public List<Game> GetHotGames()
        {
            string key = "games_hot";

            var start = DateTime.Now;
            Console.WriteLine($"⏱ GetHotGames START: {start:HH:mm:ss.fff}");

            var cached = cache.GetString(key);
            if (!string.IsNullOrEmpty(cached))
            {
                Console.WriteLine("🔥 HOT GAMES CACHE HIT");
                Console.WriteLine($"⏱ CACHE TIME: {(DateTime.Now - start).TotalMilliseconds} ms");

                return JsonSerializer.Deserialize<List<Game>>(cached);
            }

            Console.WriteLine("🐢 HOT GAMES DB");

            var dbStart = DateTime.Now;

            var data = db.Games
                .OrderByDescending(g => g.SoLuotTai)
                .Take(3)
                .ToList();

            var dbEnd = DateTime.Now;

            Console.WriteLine("🐢 HOT GAMES DONE");
            Console.WriteLine($"⏱ DB TIME: {(dbEnd - dbStart).TotalMilliseconds} ms");

            cache.SetString(key, JsonSerializer.Serialize(data, _jsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            return data;
        }

        public bool Create(Game game)
        {
            try
            {
                db.Games.Add(game);
                var result = db.SaveChanges() > 0;

                if (result)
                {
                    cache.Remove("games_all");
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool Update(Game game)
        {
            try
            {
                db.Entry(game).State = EntityState.Modified;
                var result = db.SaveChanges() > 0;

                if (result)
                {
                    cache.Remove("games_all");
                    cache.Remove($"game_{game.MaGame}");
                }

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
                var game = db.Games.Find(id);
                if (game == null) return false;

                db.Games.Remove(game);
                var result = db.SaveChanges() > 0;

                if (result)
                {
                    cache.Remove("games_all");
                    cache.Remove($"game_{id}");
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        public GameStoreContext GetDb()
        {
            return db;
        }

        public int CountGames(string search, string category)
        {
            string cacheKey = $"count_{search ?? "all"}_{category ?? "all"}";

            var start = DateTime.Now;
            Console.WriteLine($"⏱ START CountGames: {start:HH:mm:ss.fff}");

            var cached = cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                Console.WriteLine("🔥 COUNT CACHE HIT");
                Console.WriteLine($"⏱ CACHE TIME: {(DateTime.Now - start).TotalMilliseconds} ms");
                return int.Parse(cached);
            }

            Console.WriteLine("🐢 COUNT DB START");

            var dbStart = DateTime.Now;

            var query = db.Games.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(g => g.TenGame.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.MaTheLoai == category);

            int count = query.Count();

            var dbEnd = DateTime.Now;

            Console.WriteLine("🐢 COUNT DB DONE");
            Console.WriteLine($"⏱ DB TIME: {(dbEnd - dbStart).TotalMilliseconds} ms");

            cache.SetString(cacheKey, count.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            return count;
        }
    }
}
