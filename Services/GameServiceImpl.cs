using GameStore.Models;

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
    }
}
