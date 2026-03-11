using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class GameServiceImpl : GameService
    {
        private GameStoreContext db;

        public GameServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public List<Game> findAll()
        {
            return db.Games.ToList();
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
        public List<Game> FilterGames(string search, string category)
        {
            var query = db.Games.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(g => g.TenGame.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(g => g.MaTheLoai == category);

            return query.ToList();
        }

        // Game mới trong 1 tháng
        public List<Game> GetNewGames()
        {
            var oneMonthAgo = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));

            return db.Games
                .Where(g => g.NgayRaMat != null && g.NgayRaMat >= oneMonthAgo)
                .OrderByDescending(g => g.NgayRaMat)
                .Take(3)
                .ToList();
        }

        // Game hot (lượt tải cao)
        public List<Game> GetHotGames()
        {
            return db.Games
                .OrderByDescending(g => g.SoLuotTai)
                .Take(3)
                .ToList();
        }

    }
}
