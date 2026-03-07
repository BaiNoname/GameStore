using GameStore.Models;

namespace GameStore.Services
{
    public interface GameService
    {
        public List<Game> findAll();
        Game? findById(string maGame);
    }
}
