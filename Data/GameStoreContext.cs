using Microsoft.EntityFrameworkCore;

namespace GameStore.Data;

public class GameStoreContext : DbContext
{
    public GameStoreContext(DbContextOptions<GameStoreContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
}

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}