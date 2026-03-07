namespace GameStore;
using GameStore.Models;
using GameStore.Services;
using Microsoft.EntityFrameworkCore;


public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

        builder.Services.AddDbContext<GameStoreContext>(
            option => option.UseNpgsql(connectionString)
        );

        builder.Services.AddScoped<GameService, GameServiceImpl>();

        var app = builder.Build();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
            );

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
            db.Database.Migrate();
        }

        app.Run();
    }
}

