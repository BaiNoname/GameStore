using GameStore.Models;

namespace GameStore.Services
{
    public interface GameService
    {
        public List<Game> findAll();
        Game? findById(string maGame);
        public List<Game> SearchGames(string keyword);
        public List<Game> FilterGames(string search, string category);
        public List<Game> GetNewGames();
        public List<Game> GetHotGames();
    }
}
