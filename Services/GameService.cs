using GameStore.Models;

namespace GameStore.Services
{
    public interface GameService
    {
        public List<Game> findAll();
        public List<Game> findAll(string keyword, string categoryId, int page, int pageSize, out int totalPages);
        Game? findById(string maGame);
        public List<Game> SearchGames(string keyword);
        public List<Game> FilterGames(string search, string category);
        public List<Game> GetNewGames();
        public List<Game> GetHotGames();
        public bool Create(Game game);
        public bool Update(Game game);
        public bool Delete(string id);

    }
}
