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
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };

        public GameServiceImpl(GameStoreContext _db)
        {
            db = _db;
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

            return db.Games.OrderBy(g => g.MaGame).ToList();

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
            return db.Games.FirstOrDefault(x => x.MaGame == maGame);
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

            return games;
        }

        // Game mới trong 1 tháng
        public List<Game> GetNewGames(int count = 10)
        {
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            return db.Games
                .Where(g => g.NgayRaMat >= DateOnly.FromDateTime(oneMonthAgo))
                .OrderByDescending(g => g.NgayRaMat)
                .Take(count)
                .ToList();
        }

        // Game hot (lượt tải cao)
        public List<Game> GetHotGames(int count = 5)
        {
            return db.Games
                .OrderByDescending(g => g.SoLuotTai)
                .Take(count)
                .ToList();
        }

        public bool Create(Game game)
        {
            try
            {
                db.Games.Add(game);
                return db.SaveChanges() > 0;
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
                var game = db.Games.Find(id);
                if (game == null) return false;

                db.Games.Remove(game);
                return db.SaveChanges() > 0;
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
            var query = db.Games.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(g => g.TenGame.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.MaTheLoai == category);

            return query.Count();
        }
    }
}
