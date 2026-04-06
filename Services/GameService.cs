using GameStore.Models;

namespace GameStore.Services
{
    public interface GameService
    {
        public List<Game> findAll();
        public List<Game> findAll(string keyword, string categoryId, int page, int pageSize, out int totalPages);
        Game? findById(string maGame);
        public List<Game> SearchGames(string keyword);
        public List<Game> FilterGames(string search, string category, int page, int pageSize);
        public List<Game> GetNewGames(int count);
        public List<Game> GetHotGames(int count);
        public bool Create(Game game);
        public bool Update(Game game);
        public bool Delete(string id);

        GameStoreContext GetDb();

        public List<string> GetAllGameNames();
        public int CountGames(string search, string category);
    }
}
